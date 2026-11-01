/**
 * The UX pattern language, as data.
 *
 * Both `PATTERNS.md` and the gallery at `#patterns` are about this file, and `npm run patterns-check` fails if
 * the document stops mentioning something here. Written as records rather than prose so that a pattern cannot
 * be added without saying what it costs: every field below is required.
 */

export type Vocabulary = "affordance" | "status";

/** When a mark is on screen. */
export type Shown = "at rest" | "on approach" | "always";

export interface Alternative {
  what: string;
  because: string;
}

export interface Pattern {
  /** Stable. `PATTERNS.md` is checked for it. */
  id: string;
  /** The class name in the stylesheet, or the glyph where there is no class. */
  name: string;
  vocabulary: Vocabulary;
  /** What touching it does, in four words. */
  consequence: string;
  /** What it looks like. */
  mark: string;
  shown: Shown;
  /** The one line a reader needs. */
  rule: string;
  /** Why it is this and not something else. */
  why: string[];
  /** What was tried or considered, and why it lost. */
  rejected: Alternative[];
  /** What is still unsettled about this one. */
  open?: string[];
}

export interface Channel {
  channel: string;
  carries: string;
  vocabulary: Vocabulary | "neither";
}

export interface Principle {
  id: string;
  title: string;
  text: string;
}

export interface Decision {
  id: string;
  question: string;
  answer: string;
  why: string[];
  rejected: Alternative[];
}

export interface OpenQuestion {
  id: string;
  question: string;
  note: string;
}

// ── Principles ──────────────────────────────────────────────────────────────

export const PRINCIPLES: Principle[] = [
  {
    id: "by-consequence",
    title: "Organized by consequence, not by widget kind",
    text:
      "The question a reader of a dense page has to answer at a glance is not what kind of control this is, " +
      "it is what happens if they touch it. Every confusion found in this editor has been that question going " +
      "unanswered: an editor that converted a number, a readout whose two symbols sat together, a panel with " +
      "no way of saying it would close.",
  },
  {
    id: "two-vocabularies",
    title: "Two vocabularies, and no channel carries both",
    text:
      "Affordance answers what happens if I touch this. Status answers what is true about this. A channel " +
      "carries one of them and never the other. This is the rule that matters and the first one broken.",
  },
  {
    id: "spent-on-approach",
    title: "A mark is spent on approach where it can be",
    text:
      "The heavier a mark, the later it should arrive. A slot wears a fill at rest and takes its border when " +
      "the pointer does; a view control's chevron appears the same way. Forty permanently bordered fields is " +
      "a grid of rectangles, and a page of readings each carrying two chevrons is a thicket.",
  },
  {
    id: "only-where-it-leads",
    title: "An affordance is offered only where it leads somewhere",
    text:
      "A mark that does nothing useful is worse than no mark, because a reader who follows it learns to " +
      "distrust the next one. The dimensionality's exponents are reachable only in custom mode for this " +
      "reason: a named combination is already the answer.",
  },
  {
    id: "status-always-on",
    title: "Status is always visible",
    text:
      "It is true whether or not anyone is pointing at it, so it does not wait to be approached. That is also " +
      "what keeps it distinguishable from an affordance without needing a separate channel to say so.",
  },
  {
    id: "never-colour-alone",
    title: "Colour is never the only channel",
    text:
      "A verdict carries a word as well as a colour. Colour fails on a projector, in print, and for a reader " +
      "who cannot separate two of them — and this is a tool people will put on a wall in a review.",
  },
];

// ── Channels ────────────────────────────────────────────────────────────────

export const CHANNELS: Channel[] = [
  { channel: "background fill", carries: "states the model", vocabulary: "affordance" },
  { channel: "border and ring", carries: "a slot approached, or focused", vocabulary: "affordance" },
  { channel: "chevron", carries: "changes the view, or reveals", vocabulary: "affordance" },
  { channel: "dotted underline", carries: "explains", vocabulary: "affordance" },
  { channel: "colour", carries: "a verdict", vocabulary: "status" },
  { channel: "a glyph", carries: "a kind", vocabulary: "status" },
  { channel: "weight", carries: "a value is bold because it is the value", vocabulary: "neither" },
];

// ── Marks ───────────────────────────────────────────────────────────────────

export const PATTERNS: Pattern[] = [
  {
    id: "slot",
    name: ".slot",
    vocabulary: "affordance",
    consequence: "states the model",
    mark: "a background fill, a border on hover, a ring on focus",
    shown: "at rest",
    rule: "Every control inside a slot writes the model. A label inside one is still a label.",
    why: [
      "A border is the heaviest inline mark there is, so it is spent on approach rather than worn all the " +
        "time. The fill alone still reads as somewhere a value goes, and forty fills down a column line up " +
        "into something that reads as a form.",
      "The slot is the unit of consequence, so its controls carry no chrome of their own: the number, the " +
        "unit, the band's form, an operand, an exponent. A reader learns one region, not five controls.",
      "A control that writes the model is a slot whatever it looks like. The dimensionality editor was built " +
        "as a disclosure with a chevron, which said this opens where a reader needed this edits.",
    ],
    rejected: [
      {
        what: "A permanent border",
        because:
          "Unmistakable, and a grid of rectangles once a model is large. It spends the heaviest mark on the " +
          "commonest thing.",
      },
      {
        what: "An underline, like a blank on a paper form",
        because:
          "Lightest of the three, but it collides with the dotted underline that means explains, and an " +
          "empty slot with no enclosed area is nearly invisible.",
      },
      {
        what: "A raised, neumorphic surface",
        because:
          "A same-colour surface signalled by soft shadows lands near 1.1:1 where WCAG 1.4.11 wants 3:1 for " +
          "a control boundary; the near-white panel has no headroom for the light shadow and dark mode " +
          "inverts muddy; shadows need padding to read as depth and bleed together at forty rows; and a " +
          "second surface treatment collides with the fill.",
      },
    ],
  },
  {
    id: "views",
    name: ".views",
    vocabulary: "affordance",
    consequence: "changes this view only",
    mark: "a chevron",
    shown: "on approach",
    rule: "Nothing it does reaches the model. The quantity is fixed; this changes how it is read.",
    why: [
      "A readout carrying two permanent chevrons is furniture, and a page of readouts is a thicket. The " +
        "space is reserved either way so nothing shifts when it appears.",
      "Re-reading a value is never the only way to see something — the reading it replaces is already " +
        "correct — so this is the one affordance that can afford to be quiet.",
    ],
    rejected: [
      {
        what: "A permanently visible chevron",
        because: "`8.0 Pa▾ ± 0.3 Pa▾●` at rest is three pieces of furniture on one value.",
      },
      {
        what: "A dotted underline on the text",
        because:
          "What it was. It made the mark mean click here as well as hover for an explanation, on the same " +
          "page and sometimes in the same reading.",
      },
    ],
    open: [
      "Discoverability without a pointer. Nothing announces a view control to a reader who does not hover, " +
        "and on touch the first tap is the discovery.",
    ],
  },
  {
    id: "reveals",
    name: ".reveals",
    vocabulary: "affordance",
    consequence: "shows or hides detail",
    mark: "a chevron that rotates",
    shown: "always",
    rule: "Shown always, because it reports whether the thing is open as well as that it can be opened.",
    why: [
      "The deliberate exception to spending marks on approach. Open-or-shut is information at rest, so it is " +
        "worth its ink — and not showing it is exactly how the dimensionality exponents came to have no way " +
        "of saying they would close.",
      "Inside a slot it marks the way into the rest of that slot, and the panel it opens carries the slot's " +
        "fill, because it writes the model too.",
    ],
    rejected: [
      {
        what: "Nothing at all, with the reading itself as the target",
        because: "What it was. Clicking opened it, clicking again shut it, and the page never said so.",
      },
    ],
  },
  {
    id: "tells",
    name: ".tells",
    vocabulary: "affordance",
    consequence: "explains, and nothing more",
    mark: "a dotted underline and a help cursor",
    shown: "at rest",
    rule: "The only use of either. A dotted underline never means click.",
    why: [
      "A reader who learns that a dotted underline means hover never has to wonder whether it also means " +
        "something will happen.",
    ],
    rejected: [
      {
        what: "Sharing the dotted underline with view controls",
        because:
          "What it was, and the collision that started all of this: one channel meant hover for an " +
          "explanation, click to re-read, and — solid — this is the form the model keeps.",
      },
    ],
  },
  {
    id: "kept",
    name: "● kept",
    vocabulary: "status",
    consequence: "the model stores it this way",
    mark: "a filled dot after the label",
    shown: "always",
    rule: "Only meaningful where something is authored. A computed band is derived and kept nowhere.",
    why: [
      "Which of two forms the model keeps is a fact about the model, so it takes a glyph rather than " +
        "borrowing the mark that says a thing is clickable.",
    ],
    rejected: [
      {
        what: "A solid underline where a dotted one meant clickable",
        because: "What it was. Status riding an affordance channel, which is the one thing forbidden here.",
      },
    ],
  },
  {
    id: "unknown",
    name: "○ unknown",
    vocabulary: "status",
    consequence: "nothing determines it",
    mark: "a hollow dot before the word",
    shown: "always",
    rule: "Unset and unknown are different: this is a variable no equation reaches and nobody has valued.",
    why: [
      "The hollow of it is the point — there is a place for a value and nothing in it. It pairs with the " +
        "filled dot that means the opposite kind of certainty.",
    ],
    rejected: [
      { what: "The word alone", because: "In a list of forty, the eye needs something to count." },
    ],
  },
  {
    id: "verdict",
    name: "verdict colour",
    vocabulary: "status",
    consequence: "what a relationship did",
    mark: "accent holds, warn broken, pending unjudged — each with its word",
    shown: "always",
    rule: "Three outcomes, not two. A relationship that could not be judged is not one that failed.",
    why: [
      "The word is not a caption on the colour, it is the other half of the channel: colour fails on a " +
        "projector, in print, and for a reader who cannot separate two of them.",
    ],
    rejected: [
      { what: "Colour alone", because: "Unreadable in three of the places this will be looked at." },
      {
        what: "A tick and a cross",
        because:
          "Two glyphs cannot carry three outcomes without a third that reads as a question about the " +
          "reader rather than about the model.",
      },
    ],
  },
  {
    id: "at-fault",
    name: "node at fault",
    vocabulary: "status",
    consequence: "something here stops the model building",
    mark: "a bar down the left of the node, and the problem in words beneath it",
    shown: "always",
    rule: "A block-scale mark. Inline marks and block marks are different scales and may share a channel.",
    why: [
      "A border on a block is not competing with a border on a control inside it, so scale is a legitimate " +
        "discriminator — but it has to be named as one, or the next person reads the bar as an affordance.",
      "The words matter more than the bar. A `DraftProblem` names the node and says what is wrong in a " +
        "sentence the server wrote, and the bar only says which node to read.",
    ],
    rejected: [
      {
        what: "A warning glyph in the row",
        because:
          "It competes with the status glyphs for value and band, which are about the value rather than " +
          "about the node.",
      },
    ],
  },
];

// ── Cross-cutting decisions ─────────────────────────────────────────────────

export const DECISIONS: Decision[] = [
  {
    id: "editor-vs-readout",
    question: "A readout and an editor look alike. Why do they do opposite things to the number?",
    answer: "Because they hold different things, and chrome says which is which.",
    why: [
      "A readout holds a fixed quantity and a unit is a lens over it, so choosing a unit converts. An editor " +
        "holds the pair its author typed — a number and the unit it is in — so choosing a unit restates, and " +
        "the number stays put.",
      "An editor that converted would move a number its author had just entered, because filling a control " +
        "left to right puts the number in before the unit is picked.",
      "The pair is real rather than a convenient story: `ExpressionDraft.ValueUnit` persists the unit, and " +
        "`IsStoredAsAbs` persists the band's form.",
    ],
    rejected: [
      {
        what: "Making both convert",
        because: "The original behaviour, and the first thing reported: it renumbers what was just typed.",
      },
      {
        what: "A rule in the documentation and no visible difference",
        because: "A rule nobody can see is a rule nobody follows. The chrome is the rule.",
      },
    ],
  },
  {
    id: "no-raised-surfaces",
    question: "Should pressability be marked by a raised, bas-relief surface?",
    answer: "No, and the complaint behind the question was answered a different way.",
    why: [
      "The instinct was right: a nine-pixel chevron is too quiet for this edits the model. But the mark was " +
        "not missing, it was the wrong one — the control writes the model, so it is a slot.",
      "Reaching for a new mark where an existing one fits is how a vocabulary stops being small, and small " +
        "is the only property that makes it learnable.",
    ],
    rejected: [
      {
        what: "A neumorphic raised surface as a general clickability mark",
        because:
          "Contrast near 1.1:1 against 3:1 wanted; no headroom on a near-white panel and muddy when " +
          "inverted; needs padding that a dense table does not have; and a second surface treatment " +
          "collides with the fill that already means states the model.",
      },
    ],
  },
  {
    id: "marks-spent-late",
    question: "Which marks are worn at rest, and which arrive with the pointer?",
    answer: "The heaviest arrive late. Status never does.",
    why: [
      "A fill is cheap enough to wear always and still reads as a slot; a border is not. A chevron on a view " +
        "control is cheap individually and expensive in aggregate, so it arrives late too.",
      "A disclosure's chevron is the exception, because it carries state as well as affordance.",
    ],
    rejected: [
      {
        what: "Everything always visible",
        because: "Three marks on a single value, and a page of values is unreadable.",
      },
      {
        what: "Everything on hover",
        because:
          "Nothing would say where a value can be typed until the pointer arrived, and a form with no " +
          "visible fields is not a form.",
      },
    ],
  },
];

// ── What is not settled ─────────────────────────────────────────────────────

export const OPEN: OpenQuestion[] = [
  {
    id: "layout-and-density",
    question: "How is a large model arranged?",
    note:
      "The marks say what a thing will do. They say nothing about how to find it: what groups, what " +
      "collapses, what scrolls, what is worth a column. For forty variables that is the harder half, and it " +
      "is the next thing to need a language.",
  },
  {
    id: "authored-or-computed",
    question: "How does a value say whether a person typed it?",
    note:
      "An editor and a readout differ in chrome, so it is clear in a row. In a results panel every value " +
      "reads the same whether it was authored, computed, or carried in from provenance — and `IProvenance` " +
      "already knows which.",
  },
  {
    id: "stale-readings",
    question: "What does a reading look like while the server is being asked?",
    note:
      "There is one page-level pulse. Nothing says that the value you are looking at is from the draft as " +
      "it was two keystrokes ago, which is exactly when a reader is about to act on it.",
  },
  {
    id: "touch-and-keyboard",
    question: "How is a view control found without a pointer?",
    note:
      "`.views` reveals its chevron on hover and on focus, so the keyboard is covered. Touch is not: the " +
      "first tap is the discovery.",
  },
  {
    id: "unseen-at-scale",
    question: "What does any of this look like at forty rows?",
    note:
      "Nothing here has been seen at that size, on a projector, or in print — the three places the colour " +
      "rule is written for. The gallery shows one of each pattern, which is the wrong test for density.",
  },
  {
    id: "block-scale-marks",
    question: "What is the vocabulary for block-scale marks?",
    note:
      "A node at fault uses a left bar, and that is allowed because scale discriminates. But panels, " +
      "sections and groups will want marks of their own, and nothing says which channels are theirs.",
  },
];
