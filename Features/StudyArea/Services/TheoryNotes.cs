using ProfAlan.Features.StudyArea.Models;

namespace ProfAlan.Features.StudyArea.Services;

/// <summary>Course notes written against Lewis &amp; Papadimitriou, 2nd edition.</summary>
public static class TheoryNotes
{
    public static readonly IReadOnlyList<TheoryTopic> Topics =
    [
        new("languages", "Alphabets, strings and languages", "§1.7",
            "Everything in the course is a set of strings. Fix the vocabulary first.",
            [
                new(NoteKind.Definition, "An alphabet Σ is a finite set of symbols. A string over Σ is a finite sequence of symbols from Σ; the empty string is written e. Σ* is the set of all strings over Σ, and a language is any subset of Σ*.",
                    "Σ = {a, b}    Σ* = {e, a, b, aa, ab, ba, bb, aaa, ...}", "Language"),
                new(NoteKind.Paragraph, "Concatenation of strings x and y is written xy, and |w| is the length of w. For languages, L₁L₂ = { xy : x ∈ L₁, y ∈ L₂ } and the Kleene star L* is the set of all concatenations of zero or more strings from L, so e ∈ L* always."),
                new(NoteKind.Tip, "Set-builder descriptions such as L = { w ∈ {a,b}* | w starts with a and ends with bb } are the starting point of every quiz. Before building anything, list the three or four shortest strings that are in L and the shortest that are not.")
            ],
            [new("Practice with the regex checker", "sandbox?kind=regex")]),

        new("dfa", "Deterministic finite automata", "§2.1",
            "A DFA reads its input once, left to right, and is in exactly one state at a time.",
            [
                new(NoteKind.Definition, "A deterministic finite automaton is a quintuple M = (K, Σ, δ, s, F) where K is a finite set of states, Σ an alphabet, s ∈ K the initial state, F ⊆ K the set of final states, and δ : K × Σ → K the transition function.",
                    "M = (K, Σ, δ, s, F)    δ : K × Σ → K", "DFA"),
                new(NoteKind.Paragraph, "A configuration is an element of K × Σ*: the current state and the unread input. (q, aw) ⊢M (p, w) exactly when δ(q, a) = p. M accepts w iff (s, w) ⊢*M (q, e) for some q ∈ F, and L(M) is the set of strings M accepts."),
                new(NoteKind.Paragraph, "Because δ is a function on all of K × Σ, every state has exactly one outgoing arrow per symbol. A diagram that leaves some δ(q, σ) undefined is not yet a DFA; add a trap (dead) state that loops on every symbol and send the missing arrows there."),
                new(NoteKind.Example, "L = { w ∈ {a,b}* | w starts with a and ends with bb }. From s, an a moves to the main loop and a b goes to the trap state forever. Inside the loop, three states remember whether the input so far ends in a, b, or bb; only the last is final.",
                    "(q0, abb) ⊢ (q1, bb) ⊢ (q2, b) ⊢ (q3, e)    q3 ∈ F", "Starts with a, ends with bb"),
                new(NoteKind.Tip, "The DFA checker in the sandbox flags a partial δ and can add the trap state for you.")
            ],
            [new("Open the worked example", "sandbox?sample=dfa-a-bb"), new("Number of b's divisible by 3", "sandbox?sample=dfa-b-mod-3")]),

        new("nfa", "Nondeterministic finite automata", "§2.2",
            "Nondeterminism lets a machine guess; it accepts if any guess works.",
            [
                new(NoteKind.Definition, "A nondeterministic finite automaton is M = (K, Σ, Δ, s, F) where Δ, the transition relation, is a finite subset of K × Σ* × K. An arrow may read a whole string, a single symbol, or e.",
                    "Δ ⊆ K × Σ* × K", "NFA"),
                new(NoteKind.Paragraph, "(q, uw) ⊢M (p, w) whenever (q, u, p) ∈ Δ. M accepts w iff some computation reaches (f, e) with f ∈ F; other computations may get stuck or end in non-final states without affecting acceptance."),
                new(NoteKind.Paragraph, "Every NFA has an equivalent DFA (Theorem 2.2.1): the subset construction uses sets of NFA states, closed under e-moves, as DFA states. The regex checker uses the same idea to compare two languages and report a shortest string on which they differ."),
                new(NoteKind.Example, "L = { w ∈ {a,b}* | w contains bba }. Loop on a and b in q0, take a single arrow labelled bba to q1, and loop on a and b in q1. The machine guesses where bba begins.",
                    "(q0, abbab) ⊢ (q0, bbab) ⊢ (q1, b) ⊢ (q1, e)", "Contains bba")
            ],
            [new("Open the worked example", "sandbox?sample=nfa-bba"), new("Starts with bab or ends with aba", "sandbox?sample=nfa-bab-or-aba")]),

        new("regex", "Regular expressions", "§1.8 and §2.3",
            "A compact algebra for exactly the languages finite automata accept.",
            [
                new(NoteKind.Definition, "The regular expressions over Σ are built from ∅, each symbol a ∈ Σ, and the operators of union (α ∪ β), concatenation (αβ) and Kleene star (α*). The language L(α) is defined in the obvious way, with L(∅) = ∅ and L(∅*) = {e}.",
                    "Precedence: * binds tighter than concatenation, which binds tighter than ∪", "Regular expression"),
                new(NoteKind.Paragraph, "Kleene's theorem (2.3.2): a language is regular, that is accepted by some finite automaton, exactly when it is L(α) for some regular expression α. The sandbox can turn any expression into an NFA with Thompson's construction."),
                new(NoteKind.Example, "Strings that start with a and end with bb: a(a ∪ b)*bb. Strings with a number of b's divisible by 3: a*(ba*ba*ba*)*. Strings whose length is divisible by 3: ((a ∪ b)(a ∪ b)(a ∪ b))*.",
                    "a(a ∪ b)*bb", "From the midterm review"),
                new(NoteKind.Tip, "In class notes union is often written +, as in (aa + bb)(ab)*. ProfAlan accepts ∪, + and | interchangeably, and e for the empty string.")
            ],
            [new("Open the regex checker", "sandbox?kind=regex")]),

        new("cfg", "Context-free grammars", "§3.1 and §3.2",
            "Grammars generate languages by rewriting variables until only terminals remain.",
            [
                new(NoteKind.Definition, "A context-free grammar is G = (V, Σ, R, S) where V is an alphabet, Σ ⊆ V the terminals, S ∈ V − Σ the start symbol, and R ⊆ (V − Σ) × V* a finite set of rules A → u.",
                    "G = (V, Σ, R, S)", "CFG"),
                new(NoteKind.Paragraph, "uAv ⇒G uwv whenever A → w is a rule. L(G) = { w ∈ Σ* : S ⇒*G w }. A leftmost derivation always rewrites the leftmost variable, and each derivation corresponds to a parse tree."),
                new(NoteKind.Example, "S → aSb | e generates { aⁿbⁿ : n ≥ 0 }.", "S ⇒ aSb ⇒ aaSbb ⇒ aabb", "aⁿbⁿ"),
                new(NoteKind.Tip, "The CFG checker decides membership for any grammar with an Earley parser, shows a shortest leftmost derivation, draws its parse tree, and can build the equivalent PDA of Lemma 3.4.1.")
            ],
            [new("Open the grammar checker", "sandbox?kind=cfg")]),

        new("pda", "Pushdown automata", "§3.3 and §3.4",
            "A finite automaton with a stack recognises exactly the context-free languages.",
            [
                new(NoteKind.Definition, "A pushdown automaton is M = (K, Σ, Γ, Δ, s, F) where Γ is the stack alphabet and Δ ⊆ (K × (Σ ∪ {e}) × Γ*) × (K × Γ*) is the transition relation. ((p, a, β), (q, γ)) means: in state p, reading a with β on top of the stack, go to q and replace β by γ.",
                    "((p, a, β), (q, γ)) ∈ Δ", "PDA"),
                new(NoteKind.Paragraph, "Configurations are triples (q, w, α) of state, unread input and stack contents, with the top of the stack written first. M accepts w iff (s, w, e) ⊢*M (p, e, e) for some p ∈ F: the input is consumed, the stack is empty, and the state is final."),
                new(NoteKind.Example, "L = { wcwᴿ : w ∈ {a,b}* }. In s, push every a and b; on c move to f; in f, pop a symbol matching each input symbol.",
                    "(s, abcba, e) ⊢ (s, bcba, a) ⊢ (s, cba, ba) ⊢ (f, ba, ba) ⊢ (f, a, a) ⊢ (f, e, e)", "Example 3.3.1"),
                new(NoteKind.Tip, "Forgetting the empty-stack condition is the most common PDA mistake: a machine in a final state with symbols left on the stack does not accept.")
            ],
            [new("Open wcwᴿ", "sandbox?sample=pda-wcwr"), new("Open aⁿbⁿ", "sandbox?sample=pda-anbn")]),

        new("tm", "Turing machines", "§4.1 and §4.2",
            "A finite control with an unbounded tape: the textbook model of an algorithm.",
            [
                new(NoteKind.Definition, "A Turing machine is a quintuple M = (K, Σ, δ, s, H) where Σ contains the blank ⊔ and the left end symbol ▷ but not ← or →, H ⊆ K is the set of halting states, and δ : (K − H) × Σ → K × (Σ ∪ {←, →}). For every q, if δ(q, ▷) = (p, b) then b = →.",
                    "δ : (K − H) × Σ → K × (Σ ∪ {←, →})", "Turing machine"),
                new(NoteKind.Paragraph, "The tape starts with ▷ in its leftmost cell, so the head can never fall off the left end. On input w the initial configuration is (s, ▷⊔w) with the head on the blank just before w. Each step either writes a symbol on the scanned cell or moves the head one cell left or right."),
                new(NoteKind.Paragraph, "To decide a language, H = {y, n}: M accepts by halting in y and rejects by halting in n, and it must halt on every input. To semidecide, halting at all means acceptance. ProfAlan lets you mark one halting state as y; if none is marked it treats the machine as a semidecider."),
                new(NoteKind.Example, "Graph labels in ProfAlan read σ/b: on scanning σ, write b, or σ/→ and σ/← to move. To decide aⁿbⁿ, repeatedly mark the leftmost a as X and the matching b as Y; accept when only Y's remain, reject on any mismatch.",
                    "(s, ▷[⊔]aabb) ⊢ (q1, ▷⊔[a]abb) ⊢ (m, ▷⊔[X]abb) ⊢ ...", "Deciding aⁿbⁿ")
            ],
            [new("Open the aⁿbⁿ decider", "sandbox?sample=tm-anbn"), new("Rewrite every a as b", "sandbox?sample=tm-a-to-b")])
    ];
}
