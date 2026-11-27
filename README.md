# ProfAlan

An automata theory and formal languages studio for learning, simulation and assessment.
Build DFAs, NFAs, PDAs and Turing machines graphically or as transition tables, step through
every configuration, check grammars and regular expressions, and solve CodeChum-style problems
graded against visible and hidden test cases.

All definitions, configurations and acceptance conditions follow Lewis & Papadimitriou,
*Elements of the Theory of Computation*, 2nd edition.

Built with Blazor WebAssembly (.NET 10), Tailwind CSS v4 and Supabase (PostgreSQL + Auth).

## Features

| Area | Route | What it does |
| --- | --- | --- |
| Landing | `/` | Introduction, a live DFA preview, links into every area |
| Auth | `/login`, `/register` | Supabase email/password auth with Student and Instructor roles |
| Dashboard | `/dashboard` | Saved machines, problems to solve or authored, recent simulation runs |
| Sandbox | `/sandbox` | Graphical and tabular builders, step-by-step visualizer, DFA/NFA/PDA/TM/CFG/Regex checkers |
| Quiz Arena | `/quizzes`, `/quizzes/create`, `/quizzes/{id}/solve` | Problems with visible and hidden test cases and automatic grading |
| Study Area | `/study`, `/study/practice` | Theory notes with runnable examples, plus a bring-your-own-key Gemini practice generator |

Formal conventions in the engines:

- **DFA** `M = (K, Σ, δ, s, F)` with δ total. A partial δ is reported as an error, and the checker can add a trap state.
- **NFA** `Δ ⊆ K × Σ* × K`, so an arrow may read a whole string or e.
- **PDA** `M = (K, Σ, Γ, Δ, s, F)`. Accepts iff `(s, w, e) ⊢* (p, e, e)` with `p ∈ F`, which means final state *and* empty stack.
- **TM** `M = (K, Σ, δ, s, H)`. The tape starts as `▷⊔w` with the head on the blank, and δ must move → on ▷. When one halting state is marked accepting (y) the machine decides; otherwise halting means acceptance.
- **Diagrams** use the notation from the midterm review: DFA/NFA states are small dots with the name underneath and a ring for final states, and a start arrow comes in from the left. PDA and TM states are labelled circles. The canvas is infinite: scroll to zoom at the cursor, drag empty space to pan, and use the zoom bar to fit the diagram. Shapes can be dragged in from the palette, and a selected state's `+` handle can be dragged to another state to add a transition.
- **TM arrow labels** follow the class notation from the midterm review: `σ/b` writes b and `σ/→`, `σ/←` move. Every rule on an arrow is stacked as its own line.
- **Regex** accepts `∪`, `+` or `|` for union, `e` for the empty string and `∅` for the empty language.
- **CFG** variables are A–Z and the first left-hand side is the start symbol. Membership is decided with an Earley parser.

## Running locally

Prerequisites: .NET 10 SDK and Node.js 20+.

```bash
cd ProfAlan        # run commands from the folder that contains ProfAlan.csproj
npm install        # Tailwind CLI; the build also runs this if node_modules is missing
dotnet watch       # builds the CSS, then serves http://localhost:5039
```

From a parent folder, pass the project explicitly: `dotnet watch --project ProfAlan/ProfAlan.csproj`.
In VS Code, press **F5** (configuration "ProfAlan (run and open browser)") or run the default build
task **ProfAlan: run with hot reload** (Ctrl+Shift+B).

Without Supabase settings the app runs in **demo mode**: accounts, saved machines, quizzes and
submissions are kept in the browser's localStorage, and five starter problems are seeded.

### Connecting Supabase

Settings are never committed. Each build runs `scripts/write-config.mjs`, which writes the
git-ignored `wwwroot/appsettings.json` from environment variables or from a local `.env` file.

1. Copy `.env.example` to `.env`, then fill in `SUPABASE_URL` and `SUPABASE_ANON_KEY`. Find both in the dashboard under **Project Settings → API Keys**. Either the legacy anon key or the newer publishable key works. On a host such as Vercel or Netlify, set the same two variables in the build environment instead.
2. Open **SQL Editor** in the dashboard and run [`supabase/migrations/20261127000000_profalan_schema.sql`](supabase/migrations/20261127000000_profalan_schema.sql). Then run [`supabase/seed.sql`](supabase/seed.sql) for the starter problems. With the Supabase CLI you can instead run `npx supabase link --project-ref <ref>` and then `npx supabase db push`.
3. Under **Authentication → URL Configuration**, add your app origins (for example `http://localhost:5039/**` and your deployed URL) to the Redirect URLs. Confirmation emails link back to `/login`, which signs the user in automatically.
4. Run `dotnet watch`. The build log prints which mode was configured.

The anon key is public by design. Row level security protects the data:

- Users only see their own machines and runs.
- Only instructors can publish quizzes. Students' problems stay private.
- Hidden test cases can be read only by the quiz author. Solvers receive them only through the `grading_cases` function at submit time.
- Students see their own submissions. Authors see every submission to their quizzes.

### Gemini (Study Area)

Paste your own key from [Google AI Studio](https://aistudio.google.com) into the Study Area.
It is kept in `sessionStorage`, or in `localStorage` if you tick "remember". It is sent only to
Google's API and never to Supabase. Before anything generated can become a quiz, every expected
verdict is recomputed from the generated reference regex or grammar.

## Tests

```bash
dotnet test tests/ProfAlan.Tests
```

The domain layer is plain C#, so the test project compiles it directly. The tests cover the DFA, NFA, PDA and TM engines, regex parsing and equivalence, Earley membership, leftmost derivations and the grammar-to-PDA construction.

## Project structure

The code is organised as vertical slices by feature. The formal machinery sits in a framework-free `Domain` layer.

```
Domain/                     Pure C#: no Blazor, no I/O
  Automata/                 Automaton model, DfaEngine, NfaEngine, PdaEngine, TuringEngine, Tape
  Regular/                  RegexParser, EpsilonNfa (Thompson), LanguageComparer
  Grammars/                 ContextFreeGrammar, EarleyRecognizer, DerivationTracer, GrammarToPda
Features/
  Auth/                     Pages, AuthSession (AuthenticationStateProvider), Supabase and demo gateways
  Automata/                 Sandbox page, StateDiagram, TransitionGrid, ExecutionPanel, checkers, library storage
  Quizzes/                  Quiz list/editor/solve pages, QuizGrader.EvaluateTestCases, repositories
  StudyArea/                Study page, TheoryNotes, GeminiClient, ProblemVerifier, ApiKeyVault
  Dashboard/  Home/         Dashboard and landing pages
Shared/                     Layouts, nav bar, icons, palettes, toasts, browser storage, Supabase REST client
supabase/                   Schema migration with RLS, seed data
tests/ProfAlan.Tests/       xUnit tests for the domain layer
```

Each slice keeps its own `Pages/`, `Components/`, `Services/` and `Models/` folders.

## Design

- Palettes, switchable from the palette button: Platinum / French Gray / Gunmetal (default), Blue / Butter / Chocopie, Wine / Milk / Heavenly, and Link Water / Gull Grey / Blue Jay.
- Type: Cooper BT for display headings, Inter for UI text, Fira Code for alphabets, tuples, states and tapes. Cooper BT is a commercial font and is used when it is installed locally; otherwise headings fall back to Cooper Black, then Fraunces.
- All icons are inline SVG.

## Known limitations

- Grading runs in the browser. Hidden strings never appear in the UI, but they do reach the solver's browser during grading, and submitted scores are computed client-side. For high-stakes exams, move `QuizGrader` into a Supabase Edge Function.
- The Student/Instructor role is chosen at sign-up. A production deployment should approve instructors.

## Commit convention

Commits follow [Conventional Commits 1.0.0](https://www.conventionalcommits.org/en/v1.0.0/): `feat(scope): …`, `fix(scope): …`, `refactor`, `test`, `docs`, `build` and `chore`.
