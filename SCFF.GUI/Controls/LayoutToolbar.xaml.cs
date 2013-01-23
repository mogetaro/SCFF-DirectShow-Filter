﻿// Copyright 2012-2013 Alalf <alalf.iQLc_at_gmail.com>
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

/// @file SCFF.GUI/Controls/LayoutToolbar.xaml.cs
/// @copydoc SCFF::GUI::Controls::LayoutToolbar

namespace SCFF.GUI.Controls {

using System.Windows;
using System.Windows.Controls;
using SCFF.Common.GUI;

/// レイアウト編集用ツールバーを管理するUserControl
public partial class LayoutToolbar : UserControl, IBindingOptions {
  //===================================================================
  // コンストラクタ/Dispose/デストラクタ
  //===================================================================

  /// コンストラクタ
  public LayoutToolbar() {
    InitializeComponent();
  }

  //===================================================================
  // イベントハンドラ
  //===================================================================

  //-------------------------------------------------------------------
  // *Changed/Checked/Unchecked以外
  //-------------------------------------------------------------------

  /// LayoutPreview: Click
  /// @param sender 使用しない
  /// @param e 使用しない
  private void LayoutPreview_Click(object sender, RoutedEventArgs e) {
    if (!this.LayoutPreview.IsChecked.HasValue) return;
    App.Options.LayoutPreview = (bool)this.LayoutPreview.IsChecked;

    //-----------------------------------------------------------------
    // Notify self
    // Notify other controls
    Commands.ProfileVisualChanged.Execute(null, this);
    //-----------------------------------------------------------------
  }

  /// LayoutSnap: Click
  /// @param sender 使用しない
  /// @param e 使用しない
  private void LayoutSnap_Click(object sender, RoutedEventArgs e) {
    if (!this.LayoutSnap.IsChecked.HasValue) return;
    App.Options.LayoutSnap = (bool)this.LayoutSnap.IsChecked;

    //-----------------------------------------------------------------
    // Notify self
    // Notify other controls
    // Commands.ProfileVisualChanged.Execute(null, this);
    //-----------------------------------------------------------------
  }

  /// LayoutBorder: Click
  /// @param sender 使用しない
  /// @param e 使用しない
  private void LayoutBorder_Click(object sender, RoutedEventArgs e) {
    if (!this.LayoutBorder.IsChecked.HasValue) return;
    App.Options.LayoutBorder = (bool)this.LayoutBorder.IsChecked;

    //-----------------------------------------------------------------
    // Notify self
    // Notify other controls
    Commands.ProfileVisualChanged.Execute(null, this);
    //-----------------------------------------------------------------
  }

  //-------------------------------------------------------------------
  // Checked/Unchecked
  //-------------------------------------------------------------------

  //-------------------------------------------------------------------
  // *Changed/Collapsed/Expanded
  //-------------------------------------------------------------------

  //===================================================================
  // IBindingOptionsの実装
  //===================================================================

  /// @copydoc Common::GUI::IBindingOptions::CanChangeOptions
  public bool CanChangeOptions { get; private set; }
  /// @copydoc Common::GUI::IBindingOptions::OnOptionsChanged
  public void OnOptionsChanged() {
    this.CanChangeOptions = false;
    this.LayoutPreview.IsChecked = App.Options.LayoutPreview;
    this.LayoutBorder.IsChecked = App.Options.LayoutBorder;
    this.LayoutSnap.IsChecked = App.Options.LayoutSnap;
    this.CanChangeOptions = true;
  }
}
}   // namespace SCFF.GUI.Controls
