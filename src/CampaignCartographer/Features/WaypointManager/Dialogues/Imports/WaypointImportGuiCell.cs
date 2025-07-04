﻿using System.Diagnostics.CodeAnalysis;
using Cairo;
using Gantry.Core.Extensions.Api;

// ReSharper disable StringLiteralTypo

namespace ApacheTech.VintageMods.CampaignCartographer.Features.WaypointManager.Dialogues.Imports;

/// <summary>
///     A cell displayed within the cell list on the <see cref="WaypointImportDialogue"/> screen.
/// </summary>
/// <seealso cref="GuiElementTextBase" />
/// <seealso cref="IGuiElementCell" />
[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Constant Values")]
public class WaypointImportGuiCell : GuiElementTextBase, IGuiElementCell, IDisposable
{
    private LoadedTexture _cellTexture;
    private int _switchOnTextureId;
    private int _leftHighlightTextureId;
    private int _rightHighlightTextureId;

    private double _titleTextHeight;
    private const double UnscaledSwitchSize = 25.0;
    private const double UnscaledSwitchPadding = 4.0;
    private const double UnscaledRightBoxWidth = 40.0;

    /// <summary>
    ///     Initialises a new instance of the <see cref="WaypointImportGuiCell" /> class.
    /// </summary>
    /// <param name="capi">The capi.</param>
    /// <param name="cell">The cell.</param>
    /// <param name="bounds">The bounds.</param>
    public WaypointImportGuiCell(ICoreClientAPI capi, WaypointImportCellEntry cell, ElementBounds bounds) : base(capi, "", null, bounds)
    {
        Cell = cell;
        Bounds = bounds;
        _cellTexture = new LoadedTexture(capi);
        Cell.TitleFont ??= CairoFont.WhiteSmallishText();
        if (Cell.DetailTextFont != null) return;
        Cell.DetailTextFont = CairoFont.WhiteSmallText();
        Cell.DetailTextFont.Color[3] *= 0.6;
    }

    /// <summary>
    ///     Gets the cell entry associated with this GUI cell.
    /// </summary>
    public WaypointImportCellEntry Cell { get; }

    /// <summary>
    ///     Gets the bounds of this GUI cell.
    /// </summary>
    public new ElementBounds Bounds { get; }

    /// <summary>
    ///     Gets or sets the action to invoke when the left side of the cell is clicked.
    /// </summary>
    public Action<int>? OnMouseDownOnCellLeft { private get; init; }

    /// <summary>
    ///     Gets or sets the action to invoke when the right side of the cell is clicked.
    /// </summary>
    public Action<int>? OnMouseDownOnCellRight { private get; init; }

    /// <summary>
    ///     Gets or sets whether the cell is in the 'on' state.
    /// </summary>
    public bool On { get; set; } = true;

    private void GenerateEnabledTexture()
    {
        var size = scaled(UnscaledSwitchSize - 2.0 * UnscaledSwitchPadding);
        using var imageSurface = new ImageSurface(0, (int)size, (int)size);
        using var context = genContext(imageSurface);
        RoundRectangle(context, 0.0, 0.0, size, size, 2.0);
        fillWithPattern(api, context, waterTextureName);
        generateTexture(imageSurface, ref _switchOnTextureId);
    }

    private void Compose()
    {
        ComposeHover(true, ref _leftHighlightTextureId);
        ComposeHover(false, ref _rightHighlightTextureId);
        GenerateEnabledTexture();
        using var imageSurface = new ImageSurface(0, Bounds.OuterWidthInt, Bounds.OuterHeightInt);
        using var context = new Context(imageSurface);
        var num = scaled(UnscaledRightBoxWidth);
        Bounds.CalcWorldBounds();

        // Form
        const double brightness = 1.2;
        RoundRectangle(context, 0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight, 0.0);
        context.SetSourceRGBA(GuiStyle.DialogDefaultBgColor[0] * brightness, GuiStyle.DialogDefaultBgColor[1] * brightness, GuiStyle.DialogDefaultBgColor[2] * brightness, 1);
        context.Paint();

        // Main Title.
        Font = Cell.TitleFont;
        _titleTextHeight = textUtil.AutobreakAndDrawMultilineTextAt(context, Font, Cell.Title, Bounds.absPaddingX, Bounds.absPaddingY, Bounds.InnerWidth);

        // Detail Text.
        Font = Cell.DetailTextFont.WithLineHeightMultiplier(1.5);
        textUtil.AutobreakAndDrawMultilineTextAt(context, Font, Cell.DetailText, Bounds.absPaddingX + 25, Bounds.absPaddingY + _titleTextHeight + Bounds.absPaddingY + 5, Bounds.InnerWidth);

        // Top Right Text: Location of Waypoint, relative to spawn.
        var textExtents = Font.GetTextExtents(Cell.RightTopText ?? "");
        textUtil.AutobreakAndDrawMultilineTextAt(context, Font, Cell.RightTopText,
            Bounds.absPaddingX + Bounds.InnerWidth - textExtents.Width - num - scaled(10.0), Bounds.absPaddingY + scaled(Cell.RightTopOffY), textExtents.Width + 1.0, EnumTextOrientation.Right);

        context.Operator = Operator.Add;
        EmbossRoundRectangleElement(context, 0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight, false, 4, 0);
        if (Cell.Model is not null && Cell.Model.Waypoints is not null && Cell.Model.Waypoints.Count > 0)
        {
            context.SetSourceRGBA(0.0, 0.0, 0.0, 0.5);
            RoundRectangle(context, 0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight, 1.0);
            context.Fill();
        }
        var scaledSwitchSize = scaled(UnscaledSwitchSize);
        var scaledSwitchPadding = scaled(UnscaledSwitchPadding);
        var x = Bounds.absPaddingX + Bounds.InnerWidth - scaled(0.0) - scaledSwitchSize - scaledSwitchPadding;
        var y = Bounds.absPaddingY + Bounds.absPaddingY;
        context.SetSourceRGBA(0.0, 0.0, 0.0, 0.2);
        RoundRectangle(context, x, y, scaledSwitchSize, scaledSwitchSize, 3.0);
        context.Fill();
        EmbossRoundRectangleElement(context, x, y, scaledSwitchSize, scaledSwitchSize, true, 1, 2);
        generateTexture(imageSurface, ref _cellTexture);
    }

    private void ComposeHover(bool left, ref int textureId)
    {
        var imageSurface = new ImageSurface(0, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
        var context = genContext(imageSurface);
        var num = scaled(UnscaledRightBoxWidth);
        if (left)
        {
            context.NewPath();
            context.LineTo(0.0, 0.0);
            context.LineTo(Bounds.InnerWidth - num, 0.0);
            context.LineTo(Bounds.InnerWidth - num, Bounds.OuterHeight);
            context.LineTo(0.0, Bounds.OuterHeight);
            context.ClosePath();
        }
        else
        {
            context.NewPath();
            context.LineTo(Bounds.InnerWidth - num, 0.0);
            context.LineTo(Bounds.OuterWidth, 0.0);
            context.LineTo(Bounds.OuterWidth, Bounds.OuterHeight);
            context.LineTo(Bounds.InnerWidth - num, Bounds.OuterHeight);
            context.ClosePath();
        }
        context.SetSourceRGBA(0.0, 0.0, 0.0, 0.15);
        context.Fill();
        generateTexture(imageSurface, ref textureId);
        context.Dispose();
        imageSurface.Dispose();
    }

    public void OnRenderInteractiveElements(ICoreClientAPI capi, float deltaTime)
    {
        if (_cellTexture.TextureId == 0)
        {
            Compose();
        }
        api.Render.Render2DTexturePremultipliedAlpha(_cellTexture.TextureId, (int)Bounds.absX, (int)Bounds.absY, Bounds.OuterWidthInt, Bounds.OuterHeightInt);
        var mouseX = api.Input.MouseX;
        var mouseY = api.Input.MouseY;
        var vec2d = Bounds.PositionInside(mouseX, mouseY);
        if (vec2d != null)
        {
            api.Render.Render2DTexturePremultipliedAlpha(
                vec2d.X > Bounds.InnerWidth - scaled(GuiElementMainMenuCell.unscaledRightBoxWidth)
                    ? _rightHighlightTextureId
                    : _leftHighlightTextureId, (int)Bounds.absX, (int)Bounds.absY, Bounds.OuterWidth,
                Bounds.OuterHeight);
        }
        if (On)
        {
            var num = scaled(UnscaledSwitchSize - 2.0 * UnscaledSwitchPadding);
            var num2 = scaled(UnscaledSwitchPadding);
            var posX = Bounds.renderX + Bounds.InnerWidth - num + num2 - scaled(5.0);
            var posY = Bounds.renderY + scaled(8.0) + num2;
            api.Render.Render2DTexturePremultipliedAlpha(_switchOnTextureId, posX, posY, (int)num, (int)num);
            return;
        }
        api.Render.Render2DTexturePremultipliedAlpha(_rightHighlightTextureId, (int)Bounds.renderX, (int)Bounds.renderY, Bounds.OuterWidth, Bounds.OuterHeight);
        api.Render.Render2DTexturePremultipliedAlpha(_leftHighlightTextureId, (int)Bounds.renderX, (int)Bounds.renderY, Bounds.OuterWidth, Bounds.OuterHeight);
    }

    public void UpdateCellHeight()
    {
        Bounds.CalcWorldBounds();
        var paddingY = Bounds.absPaddingY / RuntimeEnv.GUIScale;
        var width = Bounds.InnerWidth;

        var innerWidth = (int)(Bounds.InnerHeight - Bounds.absPaddingY * 2.0 - 10.0);
        width -= innerWidth + 10;

        Font = Cell.TitleFont;
        text = Cell.Title;

        _titleTextHeight = textUtil.GetMultilineTextHeight(Font, Cell.Title, width) / RuntimeEnv.GUIScale;
        Font = Cell.DetailTextFont;
        text = Cell.DetailText;
        var num4 = textUtil.GetMultilineTextHeight(Font, Cell.DetailText, width) / RuntimeEnv.GUIScale;

        Bounds.fixedHeight = paddingY + _titleTextHeight + paddingY + num4 + paddingY;
        if (Bounds.fixedHeight < 50.0)
        {
            Bounds.fixedHeight = 50.0;
        }
    }

    public void OnMouseDownOnElement(MouseEvent args, int elementIndex)
    {
    }

    public void OnMouseMoveOnElement(MouseEvent args, int elementIndex)
    {
    }

    public void OnMouseUpOnElement(MouseEvent args, int elementIndex)
    {
        var mouseX = api.Input.MouseX;
        var mouseY = api.Input.MouseY;
        var vec2d = Bounds.PositionInside(mouseX, mouseY);
        api.Gui.PlaySound("menubutton_press");
        if (vec2d.X > Bounds.InnerWidth - scaled(GuiElementMainMenuCell.unscaledRightBoxWidth))
        {
            OnMouseDownOnCellRight?.Invoke(elementIndex);
            args.Handled = true;
            return;
        }
        OnMouseDownOnCellLeft?.Invoke(elementIndex);
        args.Handled = true;
    }

    public override void Dispose()
    {
        GC.SuppressFinalize(this);
        api.AsClientMain().EnqueueMainThreadTask(() =>
        {
            _cellTexture?.Dispose();
            api.Render.GLDeleteTexture(_leftHighlightTextureId);
            api.Render.GLDeleteTexture(_rightHighlightTextureId);
            api.Render.GLDeleteTexture(_switchOnTextureId);
            base.Dispose();
        }, "");
    }
}