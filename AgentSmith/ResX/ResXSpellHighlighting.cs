using System;
using AgentSmith.SpellCheck;
using AgentSmith.SpellCheck.NetSpell;

using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;

namespace AgentSmith.ResX
{
    [ConfigurableSeverityHighlighting(NAME, CSharpLanguage.Name)]
    public class ResXSpellHighlighting : SpellCheckHighlightBase
    {
        public const string NAME = "ResxSpellCheckSuggestion";
        private readonly IPsiSourceFile _file;

        public ResXSpellHighlighting(string word, IPsiSourceFile file, ISpellChecker spellChecker, DocumentRange range, IContextBoundSettingsStore settingsStore)
            : base(range, word, file.GetSolution(), spellChecker, settingsStore)
        {
            _file = file;
        }

        public IPsiSourceFile File
        {
            get { return _file; }
        }
    }
}