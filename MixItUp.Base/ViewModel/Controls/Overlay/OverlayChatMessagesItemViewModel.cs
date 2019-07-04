﻿using MixItUp.Base.Model.Overlay;
using MixItUp.Base.Util;

namespace MixItUp.Base.ViewModel.Controls.Overlay
{
    public class OverlayChatMessagesItemViewModel : OverlayListItemViewModelBase
    {
        public OverlayChatMessagesItemViewModel()
            : base()
        {
            this.HTML = OverlayChatMessagesListItemModel.HTMLTemplate;
            this.height = 24;
        }

        public OverlayChatMessagesItemViewModel(OverlayChatMessagesListItemModel item)
            : base(item.TotalToShow, item.Width, item.Height, item.TextFont, item.TextColor, item.BorderColor, item.BackgroundColor, item.Effects.EntranceAnimation, item.Effects.ExitAnimation, item.HTML)
        { }

        public override OverlayItemModelBase GetOverlayItem()
        {
            if (this.Validate())
            {
                this.TextColor = ColorSchemes.GetColorCode(this.TextColor);
                this.BorderColor = ColorSchemes.GetColorCode(this.BorderColor);
                this.BackgroundColor = ColorSchemes.GetColorCode(this.BackgroundColor);

                return new OverlayChatMessagesListItemModel(this.HTML, totalToShow, this.Font, this.width, this.height, this.BorderColor, this.BackgroundColor, this.TextColor, this.entranceAnimation, this.exitAnimation);
            }
            return null;
        }
    }
}
