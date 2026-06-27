using ChopChop.Online.Core;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChopChop.Online.UI.Auth
{
	/// <summary>
	/// View (the "V" in SCV) for the authentication screen. Binds uGUI/TextMeshPro elements to the
	/// <see cref="AuthController"/>. Pure binding — no networking, no business rules. Drop this on a
	/// Canvas panel, wire the serialized fields, and let the Zenject context inject the controller.
	/// </summary>
	public sealed class AuthView : ScvView<AuthController>
	{
		[Header("Inputs")]
		[SerializeField] private TMP_InputField _emailInput;
		[SerializeField] private TMP_InputField _passwordInput;
		[SerializeField] private TMP_InputField _displayNameInput;
		[SerializeField] private GameObject _displayNameGroup; // shown only in register mode

		[Header("Actions")]
		[SerializeField] private Button _submitButton;
		[SerializeField] private TMP_Text _submitLabel;
		[SerializeField] private Button _toggleModeButton;
		[SerializeField] private TMP_Text _toggleLabel;

		[Header("Feedback")]
		[SerializeField] private TMP_Text _statusText;
		[SerializeField] private GameObject _busyIndicator;

		protected override void Bind(AuthController c)
		{
			// View -> Controller (user typing).
			_emailInput.onValueChanged.AddListener(v => c.Email.Value = v);
			_passwordInput.onValueChanged.AddListener(v => c.Password.Value = v);
			if (_displayNameInput != null)
				_displayNameInput.onValueChanged.AddListener(v => c.DisplayName.Value = v);

			// Controller -> View (keep fields in sync without re-triggering onValueChanged).
			c.Email.Subscribe(v => SetTextSilently(_emailInput, v)).AddTo(Bag);
			c.Password.Subscribe(v => SetTextSilently(_passwordInput, v)).AddTo(Bag);
			c.DisplayName.Subscribe(v => SetTextSilently(_displayNameInput, v)).AddTo(Bag);

			// Mode-driven layout & labels.
			c.IsRegisterMode.Subscribe(on => { if (_displayNameGroup != null) _displayNameGroup.SetActive(on); }).AddTo(Bag);
			c.SubmitLabel.Subscribe(t => { if (_submitLabel != null) _submitLabel.text = t; }).AddTo(Bag);
			c.ToggleLabel.Subscribe(t => { if (_toggleLabel != null) _toggleLabel.text = t; }).AddTo(Bag);

			// Feedback.
			c.StatusMessage.Subscribe(t => { if (_statusText != null) _statusText.text = t; }).AddTo(Bag);
			c.IsBusy.Subscribe(b => { if (_busyIndicator != null) _busyIndicator.SetActive(b); }).AddTo(Bag);
			c.CanSubmit.Subscribe(can => _submitButton.interactable = can).AddTo(Bag);

			// Intent.
			_submitButton.onClick.AddListener(() => c.SubmitCommand.Execute(Unit.Default));
			_toggleModeButton.onClick.AddListener(() => c.ToggleModeCommand.Execute(Unit.Default));
		}

		private static void SetTextSilently(TMP_InputField field, string value)
		{
			if (field == null || field.text == value) return;
			field.SetTextWithoutNotify(value);
		}
	}
}
