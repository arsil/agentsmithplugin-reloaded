using System;
using System.Collections.Generic;
using System.Linq;

using AgentSmith.SpellCheck;
using AgentSmith.SpellCheck.NetSpell;

using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Intentions.Extensibility;
using JetBrains.ReSharper.Intentions.Extensibility.Menu;
using JetBrains.Util;

namespace AgentSmith.Comments {
	[QuickFix]
	public class XmlCommentSyntaxQuickFix : IQuickFix {
		private const uint MAX_SUGGESTION_COUNT = 5;

		private readonly WordIsNotInDictionaryHighlight _highlight;

		public XmlCommentSyntaxQuickFix(WordIsNotInDictionaryHighlight highlight) {
			_highlight = highlight;
		}

		#region IQuickFix Members

		public void CreateBulbItems(BulbMenu menu, Severity severity) {
			IEnumerable<IBulbAction> items = CreateItems();
			menu.ArrangeQuickFixes(items.Select(i => new Pair<IBulbAction, Severity>(i, severity)));
		}

		public bool IsAvailable(IUserDataHolder cache) {
			return true;
		}

		private IEnumerable<IBulbAction> CreateItems() {
			var items = new List<IBulbAction>();

			ISpellChecker spellChecker = _highlight.SpellChecker;

			if (spellChecker != null) {
				foreach (string suggestText in spellChecker.Suggest(_highlight.MisspelledWord, MAX_SUGGESTION_COUNT)) {
					string wordWithMisspelledWordDeleted = _highlight.Word.Remove(_highlight.Token.Start, _highlight.Token.Length);
					string newWord = wordWithMisspelledWordDeleted.Insert(_highlight.Token.Start, suggestText);
					items.Add(new ReplaceWordWithBulbItem(_highlight.DocumentRange, newWord));
				}
			}

			items.Add(new ReplaceWordWithBulbItem(_highlight.DocumentRange, String.Format("<c>{0}</c>", _highlight.Word)));
			if (spellChecker != null) {
				foreach (CustomDictionary customDict in spellChecker.CustomDictionaries) {
					items.Add(
						new AddToDictionaryBulbItem(
							_highlight.Token.Value, customDict.Name, _highlight.DocumentRange, _highlight.SettingsStore));
				}
			}
			return items;
		}
		#endregion
	}
}