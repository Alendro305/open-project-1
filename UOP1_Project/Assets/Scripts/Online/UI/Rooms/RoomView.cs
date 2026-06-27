using System.Collections.Generic;
using ChopChop.Online.Core;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChopChop.Online.UI.Rooms
{
	/// <summary>
	/// SCV view for the room lobby. Binds the create/refresh/leave controls and renders the available
	/// room list by pooling <see cref="RoomRowView"/> rows. Pure binding; no networking.
	/// </summary>
	public sealed class RoomView : ScvView<RoomController>
	{
		[Header("Create")]
		[SerializeField] private TMP_InputField _roomNameInput;
		[SerializeField] private Button _createButton;

		[Header("List")]
		[SerializeField] private Transform _listContent;
		[SerializeField] private RoomRowView _rowPrefab;
		[SerializeField] private Button _refreshButton;

		[Header("Current room")]
		[SerializeField] private GameObject _lobbyPanel;     // shown when not in a room
		[SerializeField] private GameObject _inRoomPanel;    // shown when in a room
		[SerializeField] private TMP_Text _currentRoomText;
		[SerializeField] private Button _leaveButton;

		[Header("Feedback")]
		[SerializeField] private TMP_Text _statusText;
		[SerializeField] private GameObject _busyIndicator;

		private readonly List<RoomRowView> _rows = new();

		protected override void Bind(RoomController c)
		{
			// Create.
			_roomNameInput.onValueChanged.AddListener(v => c.NewRoomName.Value = v);
			c.NewRoomName.Subscribe(v => { if (_roomNameInput.text != v) _roomNameInput.SetTextWithoutNotify(v); }).AddTo(Bag);
			_createButton.onClick.AddListener(() => c.CreateRoomCommand.Execute(Unit.Default));

			// List.
			_refreshButton.onClick.AddListener(() => c.RefreshCommand.Execute(Unit.Default));
			c.AvailableRooms.Subscribe(RenderRooms).AddTo(Bag);

			// Current room / panels.
			c.IsInRoom.Subscribe(inRoom =>
			{
				if (_lobbyPanel != null) _lobbyPanel.SetActive(!inRoom);
				if (_inRoomPanel != null) _inRoomPanel.SetActive(inRoom);
			}).AddTo(Bag);

			c.CurrentRoom.Subscribe(room =>
			{
				if (_currentRoomText != null)
					_currentRoomText.text = room == null ? "" : $"{room.Name}  ({room.Members.Count}/{room.Capacity})";
			}).AddTo(Bag);

			_leaveButton.onClick.AddListener(() => c.LeaveRoomCommand.Execute(Unit.Default));

			// Feedback.
			c.StatusMessage.Subscribe(t => { if (_statusText != null) _statusText.text = t; }).AddTo(Bag);
			c.IsBusy.Subscribe(b =>
			{
				if (_busyIndicator != null) _busyIndicator.SetActive(b);
				_createButton.interactable = !b;
				_refreshButton.interactable = !b;
			}).AddTo(Bag);
		}

		private void RenderRooms(IReadOnlyList<Models.RoomSummaryDto> rooms)
		{
			// Grow the pool as needed.
			while (_rows.Count < rooms.Count)
			{
				var row = Instantiate(_rowPrefab, _listContent);
				_rows.Add(row);
			}

			for (var i = 0; i < _rows.Count; i++)
			{
				if (i < rooms.Count)
				{
					_rows[i].gameObject.SetActive(true);
					_rows[i].Bind(rooms[i], roomId => Controller.JoinRoomCommand.Execute(roomId));
				}
				else
				{
					_rows[i].gameObject.SetActive(false);
				}
			}
		}
	}
}
