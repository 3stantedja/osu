﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;
using osu.Framework.Screens;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.UserInterface;
using osu.Game.Online.Multiplayer;
using osu.Game.Overlays;
using osu.Game.Overlays.SearchableList;
using osu.Game.Screens.Multi.Lounge.Components;
using osu.Game.Screens.Multi.Match;

namespace osu.Game.Screens.Multi.Lounge
{
    public class LoungeSubScreen : MultiplayerSubScreen
    {
        public override string Title => "Lounge";

        protected FilterControl Filter;

        private readonly Bindable<bool> initialRoomsReceived = new Bindable<bool>();

        private Container content;
        private LoadingLayer loadingLayer;

        [Resolved]
        private Bindable<Room> selectedRoom { get; set; }

        [Resolved(canBeNull: true)]
        private MusicController music { get; set; }

        private bool joiningRoom;

        [BackgroundDependencyLoader]
        private void load()
        {
            RoomsContainer roomsContainer;
            OsuScrollContainer scrollContainer;

            InternalChildren = new Drawable[]
            {
                Filter = new FilterControl { Depth = -1 },
                content = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Width = 0.55f,
                            Children = new Drawable[]
                            {
                                scrollContainer = new OsuScrollContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    ScrollbarOverlapsContent = false,
                                    Padding = new MarginPadding(10),
                                    Child = roomsContainer = new RoomsContainer
                                    {
                                        JoinRequested = joinRequested,
                                        DuplicateRoom = room =>
                                        {
                                            Room newRoom = new Room();
                                            newRoom.CopyFrom(room, true);
                                            newRoom.Name.Value = $"Copy of {room.Name.Value}";

                                            Open(newRoom);
                                        }
                                    }
                                },
                                loadingLayer = new LoadingLayer(roomsContainer),
                            }
                        },
                        new RoomInspector
                        {
                            Anchor = Anchor.TopRight,
                            Origin = Anchor.TopRight,
                            RelativeSizeAxes = Axes.Both,
                            Width = 0.45f,
                        },
                    },
                },
            };

            // scroll selected room into view on selection.
            selectedRoom.BindValueChanged(val =>
            {
                var drawable = roomsContainer.Rooms.FirstOrDefault(r => r.Room == val.NewValue);
                if (drawable != null)
                    scrollContainer.ScrollIntoView(drawable);
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            initialRoomsReceived.BindTo(RoomManager.InitialRoomsReceived);
            initialRoomsReceived.BindValueChanged(onInitialRoomsReceivedChanged, true);
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            content.Padding = new MarginPadding
            {
                Top = Filter.DrawHeight,
                Left = SearchableListOverlay.WIDTH_PADDING - DrawableRoom.SELECTION_BORDER_WIDTH + HORIZONTAL_OVERFLOW_PADDING,
                Right = SearchableListOverlay.WIDTH_PADDING + HORIZONTAL_OVERFLOW_PADDING,
            };
        }

        protected override void OnFocus(FocusEvent e)
        {
            Filter.Search.TakeFocus();
        }

        public override void OnEntering(IScreen last)
        {
            base.OnEntering(last);

            onReturning();
        }

        public override void OnResuming(IScreen last)
        {
            base.OnResuming(last);

            if (selectedRoom.Value?.RoomID.Value == null)
                selectedRoom.Value = new Room();

            music?.EnsurePlayingSomething();

            onReturning();
        }

        private void onReturning()
        {
            Filter.Search.HoldFocus = true;
        }

        public override bool OnExiting(IScreen next)
        {
            Filter.Search.HoldFocus = false;
            return base.OnExiting(next);
        }

        public override void OnSuspending(IScreen next)
        {
            base.OnSuspending(next);
            Filter.Search.HoldFocus = false;
        }

        private void joinRequested(Room room)
        {
            joiningRoom = true;
            updateLoadingLayer();

            RoomManager?.JoinRoom(room, r =>
            {
                Open(room);
                joiningRoom = false;
                updateLoadingLayer();
            }, _ =>
            {
                joiningRoom = false;
                updateLoadingLayer();
            });
        }

        private void onInitialRoomsReceivedChanged(ValueChangedEvent<bool> received) => updateLoadingLayer();

        private void updateLoadingLayer()
        {
            if (joiningRoom || !initialRoomsReceived.Value)
                loadingLayer.Show();
            else
                loadingLayer.Hide();
        }

        /// <summary>
        /// Push a room as a new subscreen.
        /// </summary>
        public void Open(Room room)
        {
            // Handles the case where a room is clicked 3 times in quick succession
            if (!this.IsCurrentScreen())
                return;

            selectedRoom.Value = room;

            this.Push(new MatchSubScreen(room));
        }
    }
}
