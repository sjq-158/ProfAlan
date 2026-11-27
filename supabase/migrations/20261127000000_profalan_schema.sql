-- ProfAlan schema. Apply with `supabase db push` or paste into the Supabase SQL editor.
-- Row level security is the only thing protecting data, because the anon key ships with the app.

create extension if not exists pgcrypto;

-- Profiles mirror auth.users and carry the role chosen at sign-up.
create table public.profiles (
    id uuid primary key references auth.users on delete cascade,
    display_name text not null default '',
    role text not null default 'student' check (role in ('student', 'instructor')),
    created_at timestamptz not null default now()
);

alter table public.profiles enable row level security;

create policy "Signed-in users can read profiles"
    on public.profiles for select to authenticated using (true);

create function public.handle_new_user()
returns trigger
language plpgsql
security definer
set search_path = ''
as $$
begin
    insert into public.profiles (id, display_name, role)
    values (
        new.id,
        coalesce(nullif(new.raw_user_meta_data ->> 'display_name', ''), split_part(new.email, '@', 1)),
        case when new.raw_user_meta_data ->> 'role' = 'instructor' then 'instructor' else 'student' end
    );
    return new;
end;
$$;

create trigger on_auth_user_created
    after insert on auth.users
    for each row execute function public.handle_new_user();

create function public.is_instructor()
returns boolean
language sql
stable
security definer
set search_path = ''
as $$
    select exists (select 1 from public.profiles where id = auth.uid() and role = 'instructor');
$$;

-- Saved machines. The definition column holds the Automaton JSON from the client.
create table public.automata (
    id uuid primary key default gen_random_uuid(),
    owner_id uuid not null default auth.uid() references auth.users on delete cascade,
    name text not null check (char_length(name) between 1 and 120),
    kind text not null check (kind in ('dfa', 'nfa', 'pda', 'tm')),
    definition jsonb not null,
    updated_at timestamptz not null default now()
);

create index automata_owner_idx on public.automata (owner_id, updated_at desc);
alter table public.automata enable row level security;

create policy "Owners manage their machines"
    on public.automata for all to authenticated
    using (owner_id = auth.uid())
    with check (owner_id = auth.uid());

create table public.simulation_runs (
    id uuid primary key default gen_random_uuid(),
    owner_id uuid not null default auth.uid() references auth.users on delete cascade,
    machine_name text not null,
    kind text not null check (kind in ('dfa', 'nfa', 'pda', 'tm')),
    input text not null,
    verdict text not null check (verdict in ('Accepted', 'Rejected', 'DidNotHalt', 'InvalidInput')),
    steps integer not null check (steps >= 0),
    created_at timestamptz not null default now()
);

create index simulation_runs_owner_idx on public.simulation_runs (owner_id, created_at desc);
alter table public.simulation_runs enable row level security;

create policy "Owners read their runs"
    on public.simulation_runs for select to authenticated using (owner_id = auth.uid());
create policy "Owners record their runs"
    on public.simulation_runs for insert to authenticated with check (owner_id = auth.uid());

-- Quizzes. Only instructors may publish; students keep private practice problems.
create table public.quizzes (
    id uuid primary key default gen_random_uuid(),
    author_id uuid default auth.uid() references auth.users on delete set null,
    author_name text not null default '',
    title text not null check (char_length(title) between 1 and 160),
    prompt text not null,
    answer_kind text not null check (answer_kind in ('dfa', 'nfa', 'pda', 'turing', 'regex', 'grammar')),
    alphabet text not null check (char_length(alphabet) between 1 and 16),
    is_published boolean not null default false,
    visible_count integer not null default 0,
    hidden_count integer not null default 0,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

alter table public.quizzes enable row level security;

create policy "Published quizzes and your own are readable"
    on public.quizzes for select to authenticated
    using (is_published or author_id = auth.uid());
create policy "Authors create quizzes"
    on public.quizzes for insert to authenticated
    with check (author_id = auth.uid() and (not is_published or public.is_instructor()));
create policy "Authors update their quizzes"
    on public.quizzes for update to authenticated
    using (author_id = auth.uid())
    with check (author_id = auth.uid() and (not is_published or public.is_instructor()));
create policy "Authors delete their quizzes"
    on public.quizzes for delete to authenticated
    using (author_id = auth.uid());

create table public.quiz_test_cases (
    id uuid primary key default gen_random_uuid(),
    quiz_id uuid not null references public.quizzes on delete cascade,
    position integer not null default 0,
    input text not null,
    should_accept boolean not null,
    is_hidden boolean not null default false
);

create index quiz_test_cases_quiz_idx on public.quiz_test_cases (quiz_id, position);
alter table public.quiz_test_cases enable row level security;

-- Solvers read visible cases of quizzes they can see. Hidden cases are readable by the author only.
create policy "Visible cases are readable; hidden cases only by the author"
    on public.quiz_test_cases for select to authenticated
    using (
        exists (
            select 1 from public.quizzes q
            where q.id = quiz_id
              and (q.author_id = auth.uid() or (q.is_published and not is_hidden))
        )
    );
create policy "Authors manage their test cases"
    on public.quiz_test_cases for all to authenticated
    using (exists (select 1 from public.quizzes q where q.id = quiz_id and q.author_id = auth.uid()))
    with check (exists (select 1 from public.quizzes q where q.id = quiz_id and q.author_id = auth.uid()));

-- The grader needs every case. This function is the only path by which a solver receives hidden
-- cases, and the client keeps them in memory without rendering them.
create function public.grading_cases(p_quiz_id uuid)
returns setof public.quiz_test_cases
language sql
stable
security definer
set search_path = ''
as $$
    select c.*
    from public.quiz_test_cases c
    join public.quizzes q on q.id = c.quiz_id
    where c.quiz_id = p_quiz_id
      and (q.is_published or q.author_id = auth.uid())
    order by c.position;
$$;

revoke execute on function public.grading_cases(uuid) from public, anon;
grant execute on function public.grading_cases(uuid) to authenticated;

create table public.submissions (
    id uuid primary key default gen_random_uuid(),
    quiz_id uuid not null references public.quizzes on delete cascade,
    student_id uuid not null default auth.uid() references auth.users on delete cascade,
    student_name text not null default '',
    answer text not null,
    passed integer not null check (passed >= 0),
    total integer not null check (total >= passed),
    created_at timestamptz not null default now()
);

create index submissions_quiz_idx on public.submissions (quiz_id, created_at desc);
create index submissions_student_idx on public.submissions (student_id, created_at desc);
alter table public.submissions enable row level security;

create policy "Students submit to quizzes they can see"
    on public.submissions for insert to authenticated
    with check (
        student_id = auth.uid()
        and exists (select 1 from public.quizzes q where q.id = quiz_id and (q.is_published or q.author_id = auth.uid()))
    );
create policy "Students read their submissions; authors read all for their quizzes"
    on public.submissions for select to authenticated
    using (
        student_id = auth.uid()
        or exists (select 1 from public.quizzes q where q.id = quiz_id and q.author_id = auth.uid())
    );
