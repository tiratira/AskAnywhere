#include "pch.h"
// pch.h should always on top! (clang-format you stop!)

#include "Utils.h"
#include "oleacc.h"

#include <codecvt>
#include <locale>

#pragma comment(lib, "Oleacc.lib")
#pragma comment(lib, "Imm32.lib")

using namespace System::Diagnostics;

namespace AskAnywhere {
System::Boolean Utils::GetCaretPosition(int % X, int % Y, int % Width,
                                        int % Height,
                                        System::IntPtr % ActiveWindow) {
  HWND hwnd;
  DWORD pid;
  DWORD tid;

  GUITHREADINFO info{};
  info.cbSize = sizeof(GUITHREADINFO);

  hwnd = GetForegroundWindow();
  tid = GetWindowThreadProcessId(hwnd, &pid);

  GetGUIThreadInfo(tid, &info);

  // IAccessible *object = nullptr;

  // if (info.hwndCaret == 0) {
  //   Debug::WriteLine("ERROR: no caret found!");
  //   return false;
  // }

  // POINT caret_pos{};

  // caret_pos.x = info.rcCaret.left;
  // caret_pos.y = info.rcCaret.bottom;

  // ClientToScreen(info.hwndCaret, &caret_pos);

  // X = caret_pos.x;
  // Y = caret_pos.y;

  // ActiveWindow = System::IntPtr(hwnd);

  // return true;

  IAccessible *object = nullptr;

  // While in word application, caret moves through animations. In this case,
  // caret can not retrieved using this api?
  if (!SUCCEEDED(AccessibleObjectFromWindow(
          info.hwndFocus, OBJID_CARET, IID_IAccessible, (void **)&object))) {
    return false;
  }

  VARIANT varCaret{};
  varCaret.vt = VT_I4;
  varCaret.lVal = CHILDID_SELF;
  long x, y, width, height;

  if (!SUCCEEDED(object->accLocation(&x, &y, &width, &height, varCaret))) {
    return false;
  }

  X = x;
  Y = y;
  Width = width;
  Height = height;
  ActiveWindow = System::IntPtr(hwnd);

  object->Release();
  return true;
}

bool Utils::SetActiveWindowAndCaret(System::IntPtr window, int x, int y) {

  auto hwnd = (HWND)window.ToPointer();

  if (!SetForegroundWindow(hwnd)) {
    Debug::WriteLine("ERROR: can not activate target window!");
    return false;
  }

  // if (!SetCaretPos(x, y)) {
  //   Debug::WriteLine("ERROR: can not set target caret!");
  //   return false;
  // }

  return true;
}

bool Utils::SendTextToCaret(System::IntPtr ptrWindow, System::String ^ text) {

  auto *hwnd = (HWND)ptrWindow.ToPointer();
  DWORD pid;
  DWORD tid;

  GUITHREADINFO info{};
  info.cbSize = sizeof(GUITHREADINFO);

  tid = GetWindowThreadProcessId(hwnd, &pid);

  GetGUIThreadInfo(tid, &info);

  for (size_t i = 0; i < text->Length; i++) {
    auto part = text[i];
    // if (part == '\n') {
    //   Debug::WriteLine("ªª––¡À£°");
    //   // INPUT inputs[2] = {};
    //   // ZeroMemory(inputs, sizeof(inputs));
    //   // inputs[0].type = INPUT_KEYBOARD;
    //   // inputs[0].ki.wVk = VK_RETURN;
    //   // inputs[1].type = INPUT_KEYBOARD;
    //   // inputs[1].ki.wVk = VK_RETURN;
    //   // inputs[1].ki.dwFlags = KEYEVENTF_KEYUP;
    //   // UINT uSent = SendInput(ARRAYSIZE(inputs), inputs, sizeof(INPUT));
    //   // if (uSent != ARRAYSIZE(inputs)) {
    //   //   Debug::WriteLine("send failed!");
    //   // }
    //   //keybd_event(VK_RETURN, 0x1C, 0, 0);
    //   //keybd_event(VK_RETURN, 0x1C, KEYEVENTF_KEYUP, 0);
    //
    // } else
    SendMessage(info.hwndFocus, WM_IME_CHAR, (WPARAM)part, 0);
  }

  return true;
}
} // namespace AskAnywhere
