﻿// Copyright 2012 Alalf <alalf.iQLc_at_gmail.com>
//
// This file is part of SCFF-DirectShow-Filter(SCFF DSF).
//
// SCFF DSF is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// SCFF DSF is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with SCFF DSF.  If not, see <http://www.gnu.org/licenses/>.

/// @file SCFF.GUI/Controls/LayoutEdit.cs
/// レイアウトエディタ

namespace SCFF.GUI.Controls {

  using SCFF.Common;
  using System;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Input;
  using System.Windows.Media;

/// レイアウトエディタ
///
/// LayoutEditImage内の座標系は([0-100],[0-100])で固定（プレビューのサイズに依存しない）
/// 逆に言うと依存させてはいけない
public partial class LayoutEdit : UserControl, IProfileToControl {

  // 1.0のままやると値が小さすぎてフォントがバグるので100倍
  private const double Scale = 100.0;

  private const double MaxImageWidth = 1.0 * Scale;
  private const double MaxImageHeight = 1.0 * Scale;
  private const double PenThickness = 0.005 * Scale;
  private const double CaptionSize = 0.04 * Scale;
  private const double CaptionMargin = PenThickness;

  private Pen dummyPen = new Pen(Brushes.DarkOrange, PenThickness);
  private Rect previewRect = new Rect(0.0, 0.0, MaxImageWidth, MaxImageHeight);

  /// コンストラクタ
  public LayoutEdit() {
    InitializeComponent();

    this.LayoutEditViewBox.Width = Constants.DummyPreviewWidth;
    this.LayoutEditViewBox.Height = Constants.DummyPreviewHeight;
    RenderOptions.SetBitmapScalingMode(this.DrawingGroup, BitmapScalingMode.LowQuality);

    // Clipping
    // 重くないか？
    this.DrawingGroup.ClipGeometry = new RectangleGeometry(this.previewRect);
    this.DrawingGroup.ClipGeometry.Freeze();

    // 使いまわすリソースはFreezeしておくとパフォーマンスがあがる
    this.dummyPen.Freeze();

    this.DrawTest();
  }

  private Rect CreateLayoutElementRect(Profile.InputLayoutElement layoutElement) {
    return new Rect() {
      X = layoutElement.BoundRelativeLeft * MaxImageWidth,
      Y = layoutElement.BoundRelativeTop * MaxImageHeight,
      Width = (layoutElement.BoundRelativeRight - layoutElement.BoundRelativeLeft) * MaxImageWidth,
      Height = (layoutElement.BoundRelativeBottom - layoutElement.BoundRelativeTop) * MaxImageHeight
    };
  }

  private FormattedText CreateLayoutElementCaption(Profile.InputLayoutElement layoutElement) {
    // 1: WindowCaption [(640x400) あれば]
    /// @todo(me) ピクセル単位の幅と高さの出力
    var layoutElementCaption = (layoutElement.Index+1).ToString() +
        ": " + layoutElement.WindowCaption;

    return new FormattedText(layoutElementCaption,
      System.Globalization.CultureInfo.CurrentUICulture,
      FlowDirection.LeftToRight,
      new Typeface("Meiryo"),
      CaptionSize,
      Brushes.DarkOrange);
  }

  private void DrawLayout(DrawingContext dc, Profile.InputLayoutElement layoutElement) {
    if (App.Options.LayoutBorder) {
      // フレームの描画
      var layoutElementRect = this.CreateLayoutElementRect(layoutElement);
      dc.DrawRectangle(Brushes.Transparent, dummyPen, layoutElementRect);

      // キャプションの描画
      var layoutElementCaption = this.CreateLayoutElementCaption(layoutElement);
      var captionPoint = new Point(layoutElementRect.X + CaptionMargin, layoutElementRect.Y + CaptionMargin);
      dc.DrawText(layoutElementCaption, captionPoint);
    }
  }

  /// 描画テスト用
  /// @todo(me) FPS制限が必要かも？でもあんまりかわらないかも
  private void DrawTest() {
    using (var dc = this.DrawingGroup.Open()) {
      // 背景描画でサイズを決める
      dc.DrawRectangle(Brushes.Black, null, this.previewRect);

      foreach (var layoutElement in App.Profile) {
        DrawLayout(dc, layoutElement);
      }
    }
  }

  //===================================================================
  // IProfileToControlの実装
  //===================================================================

  public void UpdateByProfile() {
    this.DrawTest();
  }

  public void AttachChangedEventHandlers() {
    // nop
  }

  public void DetachChangedEventHandlers() {
    // nop
  }

  //===================================================================
  // イベントハンドラ
  //===================================================================

  public const double MinimumWidth = HitTest.WEBorderThickness * 2;
  public const double MinimumHeight = HitTest.NSBorderThickness * 2;

  private Point relativeOrigin = new Point();
  private HitModes hitMode = HitModes.Neutral;

  private Point GetRelativeMousePoint(IInputElement image, MouseEventArgs e) {
    var mousePoint = e.GetPosition(image);
    return new Point(mousePoint.X / MaxImageWidth, mousePoint.Y / MaxImageHeight);
  }

  private Point GetRelativeOrigin(Point relativeMousePoint) {
    return new Point {
      X = relativeMousePoint.X - App.Profile.CurrentInputLayoutElement.BoundRelativeLeft,
      Y = relativeMousePoint.Y - App.Profile.CurrentInputLayoutElement.BoundRelativeTop
    };
  }

  /// MouseDownイベントハンドラ 
  private void LayoutEditImage_MouseDown(object sender, MouseButtonEventArgs e) {
    // 前処理
    e.Handled = true;
    var image = (IInputElement)sender;
    var relativeMousePoint = this.GetRelativeMousePoint(image, e);

    // HitTest
    int hitIndex;
    HitModes hitMode;
    if (!HitTest.TryHitTest(relativeMousePoint, out hitIndex, out hitMode)) return;

    // 現在選択中のIndexではない場合はそれに変更する
    if (hitIndex != App.Profile.CurrentInputLayoutElement.Index) {
      Debug.WriteLine("*****LayoutEdit: Change Current*****");
      Debug.WriteLine("{0:D}->{1:D} ({2:F2}, {3:F2})",
                      App.Profile.CurrentInputLayoutElement.Index,
                      hitIndex,
                      relativeMousePoint.X, relativeMousePoint.Y);

      App.Profile.ChangeCurrentIndex(hitIndex);
      Commands.ChangeCurrentLayoutElementCommand.Execute(null, null);
    }

    // マウスを押した場所を記録してマウスキャプチャー開始
    this.hitMode = hitMode;
    this.relativeOrigin = this.GetRelativeOrigin(relativeMousePoint);
    image.CaptureMouse();

    this.DrawTest();
  }

  private void LayoutEditImage_MouseMove(object sender, MouseEventArgs e) {
    // 前処理
    e.Handled = true;
    var image = (IInputElement)sender;
    var relativeMousePoint = this.GetRelativeMousePoint(image, e);

    switch (this.hitMode) {
      case HitModes.Neutral: {
        // カーソルかえるだけ
        int hitIndex;
        HitModes hitMode;
        HitTest.TryHitTest(relativeMousePoint, out hitIndex, out hitMode);
        this.Cursor = HitTest.HitModesToCursors[hitMode];
        break;
      }
      case HitModes.Move: {
        // 移動
        var nextLeft = relativeMousePoint.X - relativeOrigin.X;
        var nextTop = relativeMousePoint.Y - relativeOrigin.Y;

        if (nextLeft < 0.0) nextLeft = 0.0;
        if (nextTop < 0.0) nextTop = 0.0;

        if (nextLeft + MinimumWidth <= App.Profile.CurrentInputLayoutElement.BoundRelativeRight) {
          // ok
          App.Profile.CurrentOutputLayoutElement.BoundRelativeLeft = nextLeft;
        }
        if (nextTop + MinimumHeight <= App.Profile.CurrentInputLayoutElement.BoundRelativeBottom) {
          // ok
          App.Profile.CurrentOutputLayoutElement.BoundRelativeTop = nextTop;
        }
      
        /// @todo(me) 変更をMainWindowに通知
        Commands.ChangeLayoutParameterCommand.Execute(null, null);

        this.DrawTest();
        break;
      }
    }

    if (this.hitMode == HitModes.Neutral) {

      return;
    } else if (this.hitMode == HitModes.Move) {
      
    }
  }

  private void LayoutEditImage_MouseUp(object sender, MouseButtonEventArgs e) {
    e.Handled = true;
    if (this.hitMode != HitModes.Neutral) {
      this.LayoutEditImage.ReleaseMouseCapture();
      this.hitMode = HitModes.Neutral;
    }
  }
}
}
