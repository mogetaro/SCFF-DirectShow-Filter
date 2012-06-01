﻿// Copyright 2012 Alalf <alalf.iQLc_at_gmail.com>
//
// This file is part of SCFF DSF.
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

/// @file scff-app/scff-app.cs
/// @brief MVCパターンにおけるControllerの定義

namespace scff_app {

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;
  using System.Diagnostics;

/// @brief Form1(メインウィンドウ)から利用する実装クラス
partial class SCFFApp {

  // 定数
  public const int kDefaultBoundWidth = 640;
  public const int kDefaultBoundHeight = 360;
  public const int kMaxLayoutElements = scff_interprocess.Interprocess.kMaxComplexLayoutElements;
  const string kSCFFSourceGUID = "D64DB8AA-9055-418F-AFE9-A080A4FAE47A";
  const string kRegistryKey = "CLSID\\{" + kSCFFSourceGUID + "}";

  //-------------------------------------------------------------------

  /// @brief コンストラクタ
  public SCFFApp(BindingSource entries, BindingSource layout_parameters) {
    entries_ = entries;
    layout_parameters_ = layout_parameters;

    interprocess_ = new scff_interprocess.Interprocess();
    directory_ = new data.Directory();
    message_ = new data.Message();
  }

  /// @brief メインフォームのLoad時に呼ばれることを想定
  public void OnLoad() {
    // ディレクトリとBindingSourceを共有メモリから更新
    UpdateDirectory();

    // BindingSourceにレイアウトをひとつだけ追加しておく
    layout_parameters_.AddNew();
  }

  /// @brief 起動時のチェックを行うメソッド
  public bool CheckEnvironment() {
    //------------------
    // 32bit版のチェック
    //------------------
    bool is_correctly_installed_x86 = false;
    bool is_dll_found_x86 = false;
    string dll_path_x86 = "";
    try {
      RegistryKey scff_dsf_key =
          RegistryKey.OpenBaseKey(
              RegistryHive.ClassesRoot,
              RegistryView.Registry32).OpenSubKey(kRegistryKey);
      if (scff_dsf_key != null) {
        is_correctly_installed_x86 = true;
      }

      RegistryKey scff_dsf_path_key = scff_dsf_key.OpenSubKey("InprocServer32");
      dll_path_x86 = scff_dsf_path_key.GetValue("").ToString();
      if (File.Exists(dll_path_x86)) {
        is_dll_found_x86 = true;
      }
    } catch {
      // 念のためエラーが出た場合も考慮
    }

    //------------------
    // 64bit版のチェック
    //------------------
    bool is_correctly_installed_amd64 = false;
    bool is_dll_found_amd64 = false;
    string dll_path_amd64 = "";
    try {
      RegistryKey scff_dsf_key =
          RegistryKey.OpenBaseKey(
              RegistryHive.ClassesRoot,
              RegistryView.Registry64).OpenSubKey(kRegistryKey);
      if (scff_dsf_key != null) {
        is_correctly_installed_amd64 = true;
      }

      RegistryKey scff_dsf_path_key = scff_dsf_key.OpenSubKey("InprocServer32");
      dll_path_amd64 = scff_dsf_path_key.GetValue("").ToString();
      if (File.Exists(dll_path_amd64)) {
        is_dll_found_amd64 = true;
      }
    } catch {
      // 念のためエラーが出た場合も考慮
    }

    //----------------------
    // エラーダイアログの表示
    // （若干不正確だがないよりましだろう）
    //----------------------
    if (!is_correctly_installed_x86 && !is_correctly_installed_amd64) {
      // 32bit版も64bit版もインストールされていない場合
      MessageBox.Show("scff-*.ax is not correctly installed.\nPlease re-install SCFF DirectShow Filter.",
                      "Not correctly installed",
                      MessageBoxButtons.OK,
                      MessageBoxIcon.Error);
      return false;
    }

    if (!is_dll_found_x86 && !is_dll_found_amd64) {
      // 32bit版のDLLも64bit版のDLLも指定された場所に存在していない場合
      string message = "scff-*.ax is not found:\n";
      message += "\n";
      message += "  32bit: " + dll_path_x86 + "\n";
      message += "  64bit: " + dll_path_amd64 + "\n"; 
      message += "\n";
      message += "Check your SCFF directory.";
      MessageBox.Show(message,
                      "DLL is not found",
                      MessageBoxButtons.OK,
                      MessageBoxIcon.Error);
      return false;
    }

    //------------------
    // カラーチェック
    //------------------
    if (Screen.PrimaryScreen.BitsPerPixel != 32) {
      MessageBox.Show("SCFF requires primary screen is configured 32bit color mode.",
                      "Not 32bit color mode",
                      MessageBoxButtons.OK, MessageBoxIcon.Error);
      return false;
    }

    // 起動OK
    return true;
  }

  //-------------------------------------------------------------------
  // UI要素作成用
  //-------------------------------------------------------------------

  public List<KeyValuePair<scff_interprocess.SWScaleFlags,string>> ResizeMethodList {
    get {
      return new List<KeyValuePair<scff_interprocess.SWScaleFlags,string>>(data.SWScaleConfig.ResizeMethodList);
    }
  }

  //-------------------------------------------------------------------
  // Directory
  //-------------------------------------------------------------------

  public void UpdateDirectory() {
    // 共有メモリにアクセス
    interprocess_.InitDirectory();
    scff_interprocess.Directory interprocess_directory;
    interprocess_.GetDirectory(out interprocess_directory);

    // Directoryに設定
    directory_.LoadFromInterprocess(interprocess_directory);

    // BindingSourceを更新
    directory_.Update(entries_);
  }

  //-------------------------------------------------------------------
  // Message
  //-------------------------------------------------------------------

  void Send(bool show_message) {
    if (entries_.Count == 0) {
      // 書き込み先が存在しない
      if (show_message) {
        MessageBox.Show("No process to send message.");
      }
      return;
    }

    data.Entry current_entry = (data.Entry)entries_.Current;

    try {
      /// @warning DWORD->int変換！オーバーフローの可能性あり
      Process.GetProcessById((int)current_entry.ProcessID);
    } catch {
      // プロセスが存在しない場合
      if (show_message) {
        MessageBox.Show("Cannot find process(" + current_entry.ProcessID + ").");
      }
      return;
    }

    // Messageを変換
    scff_interprocess.Message interprocess_message =
        message_.ToInterprocess(current_entry.SampleWidth, current_entry.SampleHeight);
    
    // 共有メモリにアクセス
    interprocess_.InitMessage(current_entry.ProcessID);
    interprocess_.SendMessage(interprocess_message);
  }

  public void SendNull(bool show_message) {
    message_.Reset();
    Send(show_message);
  }

  public void SendMessage(bool show_message) {
    message_.Load(layout_parameters_);
    if (!message_.Validate(show_message)) {
      return;
    }
    Send(show_message);
  }

  //-------------------------------------------------------------------
  // Window
  //-------------------------------------------------------------------

  public void SetDesktopWindow() {
    ((data.LayoutParameter)layout_parameters_.Current).SetWindow(ExternalAPI.GetDesktopWindow());
  }

  public void SetWindowFromPoint(int screen_x, int screen_y) {
    UIntPtr window = ExternalAPI.WindowFromPoint(screen_x, screen_y);
    ((data.LayoutParameter)layout_parameters_.Current).SetWindow(window);
  }

  //===================================================================
  // メンバ変数
  //===================================================================

  BindingSource entries_;
  BindingSource layout_parameters_;

  scff_interprocess.Interprocess interprocess_;
  data.Directory directory_;
  data.Message message_;
}
}   // namespace scff_app