using System.Windows.Controls;

using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.UI.Application;
using JetBrains.UI.Options;
using JetBrains.UI.Options.Helpers;

namespace AgentSmith.Options {
	[OptionsPage(PID, "General", typeof(OptionsThemedIcons.SamplePage), ParentId = XmlDocumentationOptionsPage.PID)]
	public class XmlDocumentationGeneralOptionsPage : AOptionsPage {

		public const string PID = "AgentSmithXmlDocumentationGEneralId";

		private OptionsSettingsSmartContext _settings;

		private XmlDocumentationOptionsUI _optionsUI;

		public XmlDocumentationGeneralOptionsPage([NotNull] Lifetime lifetime, OptionsSettingsSmartContext settingsSmartContext, IUIApplication environment)
			: base(lifetime, environment, PID) {
			_settings = settingsSmartContext;
			_optionsUI = new XmlDocumentationOptionsUI();
			this.Control = _optionsUI;

			settingsSmartContext.SetBinding<XmlDocumentationSettings, string>(
				lifetime, x => x.DictionaryName, _optionsUI.txtDictionaryName, TextBox.TextProperty);
			settingsSmartContext.SetBinding<XmlDocumentationSettings, bool?>(
				lifetime, x => x.SuppressIfBaseHasComment, _optionsUI.chkSuppressIfBaseHasComment, CheckBox.IsCheckedProperty);
			settingsSmartContext.SetBinding<XmlDocumentationSettings, int>(
				lifetime, x => x.MaxCharactersPerLine, _optionsUI.txtMaxCharsPerLine, IntegerTextBox.ValueProperty);
			settingsSmartContext.SetBinding<XmlDocumentationSettings, string>(
				lifetime, x => x.WordsToIgnore, _optionsUI.txtWordsToIgnore, TextBox.TextProperty);
			settingsSmartContext.SetBinding<XmlDocumentationSettings, string>(
				lifetime, x => x.WordsToIgnoreForMetatagging, _optionsUI.txtWordsToIgnoreForMetatagging, TextBox.TextProperty);

		}
	}
}