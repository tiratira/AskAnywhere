#include "pch.h"
// pch.h should always on top! (clang-format you stop!)

#include "Utils.h"
#include "oleacc.h"

#pragma comment(lib, "Oleacc.lib")

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

  IAccessible *object = nullptr;

  // While in word application, caret moves through animations. In this case, caret
  // can not retrieved using this api?
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
} // namespace AskAnywhere
