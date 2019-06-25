using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Automata
{
    /// <summary>
    /// Exeption thrown by the automata constructions
    /// </summary>
    public class AutomataException : Exception
    {
        /// <summary>
        /// the kind of exception
        /// </summary>
        public readonly AutomataExceptionKind kind;

        /// <summary>
        /// construct an exception
        /// </summary>
        public AutomataException(string message, Exception innerException)
            : base(message, innerException)
        {
            kind = AutomataExceptionKind.Unspecified;
        }

        /// <summary>
        /// construct an exception with given message
        /// </summary>
        public AutomataException(string message)
            : base(message)
        {
            kind = AutomataExceptionKind.Unspecified;
        }

        /// <summary>
        /// construct an exception with given kind
        /// </summary>
        public AutomataException(AutomataExceptionKind kind)
            : base(GetMessage(kind))
        {
            this.kind = kind;
        }

        /// <summary>
        /// construct an exception with given kind and inner exception
        /// </summary>
        public AutomataException(AutomataExceptionKind kind, Exception innerException)
            : base(GetMessage(kind), innerException)
        {
            this.kind = kind;
        }

        private static string GetMessage(AutomataExceptionKind kind)
        {
            switch (kind)
            {
                case AutomataExceptionKind.AutomatonIsNondeterministic:
                    return AutomatonIsNotDeterministic;
                case AutomataExceptionKind.AutomatonIsNotEpsilonfree:
                    return AutomatonIsNotEpsilonfree;
                case AutomataExceptionKind.AutomatonMustBeNonempty:
                    return AutomatonMustBeNonempty;
                case AutomataExceptionKind.AutomatonMustNotContainDeadStates:
                    return AutomatonMustNotContainDeadStates;
                case AutomataExceptionKind.CharacterEncodingIsUnspecified:
                    return CharacterEncodingIsUnspecified;
                case AutomataExceptionKind.ContextCannotBePopped:
                    return ContextCannotBePopped;
                case AutomataExceptionKind.InputSortsMustBeIdentical:
                    return InputSortsMustBeIdentical;
                case AutomataExceptionKind.InvalidAutomatonName:
                    return InvalidAutomatonName;
                case AutomataExceptionKind.MisplacedEndAnchor:
                    return MisplacedEndAnchor;
                case AutomataExceptionKind.MisplacedStartAnchor:
                    return MisplacedStartAnchor;
                case AutomataExceptionKind.NoFinalState:
                    return NoFinalState;
                case AutomataExceptionKind.RegexConstructNotSupported:
                    return RegexConstructNotSupported;
                case AutomataExceptionKind.CharSetMustBeNonempty:
                    return CharSetMustBeNonempty;
                case AutomataExceptionKind.SolversAreNotIdentical:
                    return SolversMustBeIdentical;
                case AutomataExceptionKind.TheoryIsNotAsserted:
                    return TheoryMustBeAsserted;
                case AutomataExceptionKind.UnrecognizedRegex:
                    return UnrecognizedRegex;
                case AutomataExceptionKind.InternalError:
                    return InternalError;
                default:
                    return kind.ToString();
            }
        }

        public const string RegexConstructNotSupported =
            "The following constructs are currently not supported: anchors \\G, \\B, named groups, lookahead, lookbehind, as-few-times-as-possible quantifiers, backreferences, conditional alternation, substitution";
        public const string MisplacedEndAnchor =
            "The anchor \\z, \\Z or $ is not supported if it is followed by other regex patterns or is nested in a loop";
        public const string MisplacedStartAnchor =
            "The anchor \\A or ^ is not supported if it is preceded by other regex patterns or is nested in a loop";
        public const string UnrecognizedRegex =
            "Unrecognized regex construct";
        public const string AutomatonIsNotDeterministic =
            "Automaton is not deterministic";
        public const string AutomatonMustNotContainDeadStates =
            "Automaton must not contain dead states";
        public const string AutomatonIsNotEpsilonfree =
            "Automaton is not epsilonfree";
        public const string NoFinalState =
            "There is no final state";
        public const string AutomatonMustBeNonempty =
            "Automaton must be nonempty";
        public const string ContextCannotBePopped =
            "Context cannot be popped";
        public const string CharSetMustBeNonempty =
            "Set must be nonempty";
        public const string TheoryMustBeAsserted =
            "Theory that defines the Acceptor has not been asserted";
        public const string SolversMustBeIdentical =
            "Solvers are not identical";
        public const string InputSortsMustBeIdentical =
            "Input sorts are not identical";
        public const string CharacterEncodingIsUnspecified =
            "Character encoding is unspecified";
        public const string InvalidAutomatonName =
            "Name must be different from the empty string and null";
        public const string InternalError =
            "Internal error";
    }


    /// <summary>
    /// Kinds of exceptions that may be thrown by the Automata library operations.
    /// </summary>
    public enum AutomataExceptionKind
    {
        RegexConstructNotSupported,
        MisplacedEndAnchor,
        MisplacedStartAnchor,
        UnrecognizedRegex,
        AutomatonIsNondeterministic,
        AutomatonMustNotContainDeadStates,
        AutomatonIsNotEpsilonfree,
        NoFinalState,
        AutomatonMustBeNonempty,
        ContextCannotBePopped,
        CharSetMustBeNonempty,
        TheoryIsNotAsserted,
        SolversAreNotIdentical,
        InputSortsMustBeIdentical,
        CharacterEncodingIsUnspecified,
        InvalidAutomatonName,
        InternalError,
        AssertAxiom_UnexpectedFuncSymbol,
        AssertAxiom_IncorrectVariables,
        AssertAxiom_SortMismatch,
        Unspecified,
        FunctionIsAlreadyDeclared,
        FunctionDeclarationIsInvalid,
        FunctionDeclarationIsNotInScope,
        EpsilonMovesAreNotSupportedInSTs,
        SortMismatch,
        TheoryIsNotInScope,
        SortMismatchInComposition,
        UnspecifiedOperatorDeclarationKind,
        UnExpectedNrOfOperands,
        ModelDoesNotExist,
        UnexpectedExprKindInPrettyPrinter,
        InvalidArguments,
        InvalidRegex,
        CharSetMustBeNontrivial,
        InvalidRegexCharacterClass,
        CompactSerializationNodeLimitViolation,
        CompactSerializationBitLimitViolation,
        CompactDeserializationError,
        SetIsFull,
        SetIsEmpty,
        RankedAlphabet_IsAlreadyDefined,
        RankedAlphabet_IsUndefined,
        RankedAlphabet_UnknownSymbol,
        RankedAlphabet_ChildAccessorIsOutOufBounds,
        RankedAlphabet_ContainsNoLeaf,
        RankedAlphabet_IsInvalid,
        RankedAlphabet_UnrecognizedAlphabetSort,
        UnexpectedExprKind,
        UnexpectedFunctionSymbol,
        InvalidArgument,
        TreeTheory_UnexpectedVariable,
        TreeTheory_UnexpectedTransductionRuleOutput,
        TreeTheory_UnexpectedOutput,
        TreeTheory_UnexpectedTransductionRule,
        TreeTransducer_SortMismatch,
        TreeTransducer_SolverMismatch,
        GrammarNotInGNF,
        RankedAlphabet_IvalidNrOfSubtrees,
        TreeTransducer_InvalidStateId,
        TreeTheory_RankMismatch,
        TreeTheory_UnexpectedState,
        TreeTheory_UnexpectedAcceptorRule,
        TreeTheory_UnexpectedRule,
        TreeTransducer_ArgumentIsNotAcceptor,
        RankedAlphabet_InvalidAcceptorRuleArguments,
        TreeTransducer_UndefinedState,
        TreeTransducer_EmptySetOfRules,
        TreeTransducer_NotAcceptor,
        TreeTransducer_UnexpectedOutputExpr,
        UnexpectedDeclKind,
        Fast_AttributeMustBeTuple,
        InvalidEnumDeclaration,
        UnexpectedSort,
        UndefinedEnum,
        UndefinedEnumElement,
        EnumWithThisNameIsAlreadyDefined,
        TreeTransducerR_InvalidLookaheadAutomaton,
        TreeTransducerR_AlphabetMismatch,
        TreeRule_InvalidChildIdentifier,
        TreeTransducer_OutputIsUndefined,
        TreeTransducer_InitialStateIsUnused,
        TreeTransducer_ArgumentIsNotClean,
        RankedAlphabet_InvalidNrOfFields,
        RankedAlphabet_InvalidField,
        RankedAlphabet_InvalidSubtree,
        TreeTransducer_IsEmpty,
        Fast_DefNotFound,
        TreeTransducer_InvalidUseOfIdentityState,
        MkIdRule_InternalError,
        MkIdRule_RuleIsNotAcceptorRule,
        MkIdRule_RuleIsNotFlat,
        TreeTransducer_InvalidUseOfAcceptorState,
        NotRegisterFree,
        AutomatonMustBeLoopFreeAndDeterministic,
        ProbabilitiesHaveNotBeenComputed,
        AutomatonInvalidState,
        AutomatonInvalidMove,
        UniformGenerationRequiresFiniteDomain,
        InvalidSwitchCase,
        ConditionCannotBeConvertedToCharSet,
        TreeTransducer_NotMonadic,
        TreeTransducer_NotSFAcompatible,
        TreeTransducer_NotClean,
        RankedAlphabet_IsNotSFAcompatible,
        RankedAlphabet_RankIsOutOfBounds,
        InternalError_Determinization,
        AutomatonInvalidInput,
        STMustBeLifted,
        InvalidSubstitution,
        InvalidSTbRuleOperation,
        OverlappingPatternsNotSupported,
        CaptureIsInfeasibleAsBoolean,
        FinalStateMustBeSink,
        LabelIsNotConvertableToBvSet,
        ExprIsNotIntSymbol,
        ExpectingConstIntSymbol,
        ExpectingVariable,
        NotListSort,
        IncompatibleAlgebras,
        IncompatibleBounds,
        NotSupported,
        BooleanAlgebraIsNotAtomic,
        OrdinalIsTooLarge,
        PredicateIsNotSingleton,
        UnexpectedMTBDDTerminal,
        InvalidWS1Sformula,
        InvalidWS1Sformula_UnknownVariable,
        InvalidWS1Sformula_FormulaNotClosed,
        AlgebraMustBeCharSetSolver,
        CompactSerializationNotSupportedForMTBDDs,
        MTBDDsNotSupportedForThisOperation,
        BDDSerializationNodeLimitViolation,
        BDDSerializationBitLimitViolation,
        BDDDeserializationError,
        InvalidName,
        InvalidPath,
        InvalidArgument_MustBeNonempty,
        NotCartesianAlgebra,
        IteBagError,
        AutomataConversionFailed,
        IncompatibeBitvectors,
        BitOutOfRange,
        InternalError_RegexAutomaton,
        InvalidArgument_IndexOutOfRange,
        InternalError_SymbolicRegex,
        MustNotAcceptEmptyString,
        MatcherIsUninitialized,
        SerializationNotSupported,
        NrOfMintermsCanBeAtMost64,
        CounterIndexOutOfRange,
        ExpectingConjunction,
        InternalError_ComposeCounterUpdates,
        SequenceIsEmpty,
        IncompatibleGrammarTerminalType,
        StartSymbolOfContextFreeGrammarIsUnreachable,
        InternalError_LazyProductAutomaton,
        AutomatonMissingTransition,
        InternalError_DistinguishingSequenceGeneration,
        InvalidCall,
        ArgumentMustBeFinalState,
        InternalError_GenerateCounterMinterms,
    }
}