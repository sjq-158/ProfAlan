-- Starter problems published by ProfAlan (no author account).
-- Expected verdicts follow the reference language noted beside each problem.

with q as (
    insert into public.quizzes (author_id, author_name, title, prompt, answer_kind, alphabet, is_published, visible_count, hidden_count)
    values (null, 'ProfAlan', 'Starts with a and ends with bb',
            'Construct a DFA for L = { w ∈ {a,b}* | w starts with a and ends with bb }.', 'dfa', 'ab', true, 4, 6)
    returning id
) -- reference: a(a ∪ b)*bb
insert into public.quiz_test_cases (quiz_id, position, input, should_accept, is_hidden)
select q.id, v.position, v.input, v.should_accept, v.is_hidden from q, (values
    (0, 'abb', true, false), (1, 'ab', false, false), (2, 'aabb', true, false), (3, 'babb', false, false),
    (4, 'abbabb', true, true), (5, 'abba', false, true), (6, 'a', false, true), (7, 'bb', false, true),
    (8, '', false, true), (9, 'aabbb', true, true)
) as v(position, input, should_accept, is_hidden);

with q as (
    insert into public.quizzes (author_id, author_name, title, prompt, answer_kind, alphabet, is_published, visible_count, hidden_count)
    values (null, 'ProfAlan', 'Number of b''s divisible by 3',
            'Construct a DFA for L = { w ∈ {a,b}* | the number of b''s in w is divisible by 3 }.', 'dfa', 'ab', true, 4, 5)
    returning id
) -- reference: a*(ba*ba*ba*)*
insert into public.quiz_test_cases (quiz_id, position, input, should_accept, is_hidden)
select q.id, v.position, v.input, v.should_accept, v.is_hidden from q, (values
    (0, '', true, false), (1, 'bbb', true, false), (2, 'ab', false, false), (3, 'abab', false, false),
    (4, 'bbbbbb', true, true), (5, 'babab', true, true), (6, 'b', false, true), (7, 'abbbbab', false, true),
    (8, 'aaa', true, true)
) as v(position, input, should_accept, is_hidden);

with q as (
    insert into public.quizzes (author_id, author_name, title, prompt, answer_kind, alphabet, is_published, visible_count, hidden_count)
    values (null, 'ProfAlan', 'Contains bba',
            'Construct an NFA for L = { w ∈ {a,b}* | w contains bba as a substring }.', 'nfa', 'ab', true, 4, 5)
    returning id
) -- reference: (a ∪ b)*bba(a ∪ b)*
insert into public.quiz_test_cases (quiz_id, position, input, should_accept, is_hidden)
select q.id, v.position, v.input, v.should_accept, v.is_hidden from q, (values
    (0, 'bba', true, false), (1, 'abba', true, false), (2, 'bb', false, false), (3, 'aba', false, false),
    (4, 'bbba', true, true), (5, 'babba', true, true), (6, 'bab', false, true), (7, '', false, true),
    (8, 'aabbaa', true, true)
) as v(position, input, should_accept, is_hidden);

with q as (
    insert into public.quizzes (author_id, author_name, title, prompt, answer_kind, alphabet, is_published, visible_count, hidden_count)
    values (null, 'ProfAlan', 'aa or bb, then ab blocks',
            'Write a regular expression for strings over {a,b} that begin with aa or bb followed by any number of ab blocks.', 'regex', 'ab', true, 4, 5)
    returning id
) -- reference: (aa ∪ bb)(ab)*
insert into public.quiz_test_cases (quiz_id, position, input, should_accept, is_hidden)
select q.id, v.position, v.input, v.should_accept, v.is_hidden from q, (values
    (0, 'aa', true, false), (1, 'bbab', true, false), (2, 'ab', false, false), (3, 'aab', false, false),
    (4, 'aaabab', true, true), (5, 'bb', true, true), (6, 'abab', false, true), (7, '', false, true),
    (8, 'bba', false, true)
) as v(position, input, should_accept, is_hidden);

with q as (
    insert into public.quizzes (author_id, author_name, title, prompt, answer_kind, alphabet, is_published, visible_count, hidden_count)
    values (null, 'ProfAlan', 'Equal a''s then b''s',
            'Construct a PDA for L = { aⁿbⁿ : n ≥ 0 }. Accept by final state with an empty stack.', 'pda', 'ab', true, 4, 5)
    returning id
) -- reference: S → aSb | e
insert into public.quiz_test_cases (quiz_id, position, input, should_accept, is_hidden)
select q.id, v.position, v.input, v.should_accept, v.is_hidden from q, (values
    (0, '', true, false), (1, 'ab', true, false), (2, 'aab', false, false), (3, 'ba', false, false),
    (4, 'aaabbb', true, true), (5, 'abab', false, true), (6, 'aaaabbbb', true, true), (7, 'b', false, true),
    (8, 'aabbb', false, true)
) as v(position, input, should_accept, is_hidden);
