using System;
using ChopChop.Online.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChopChop.Online.UI.Rooms
{
	/// <summary>A single row in the room list. Plain presentational widget (not an SCV view).</summary>
	public sealed class RoomRowView : MonoBehaviour
	{
		[SerializeField] private TMP_Text _nameText;
		[SerializeField] private TMP_Text _membersText;
		[SerializeField] private Button _joinButton;

		public void Bind(RoomSummaryDto room, Action<string> onJoin)
		{
			if (_nameText != null) _nameText.text = room.Name;
			if (_membersText != null) _membersText.text = $"{room.MemberCount}/{room.Capacity}";

			_joinButton.onClick.RemoveAllListeners();
			var full = room.MemberCount >= room.Capacity;
			_joinButton.interactable = !full;
			_joinButton.onClick.AddListener(() => onJoin(room.Id));
		}
	}
}
