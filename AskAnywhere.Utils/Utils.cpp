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

void Utils::MarshalString(System::String ^ s, std::string &os) {
  using namespace System::Runtime::InteropServices;
  const char *chars =
      (const char *)(Marshal::StringToHGlobalAnsi(s)).ToPointer();
  os = chars;
  Marshal::FreeHGlobal(System::IntPtr((void *)chars));
}

bool Utils::SendTextToCaret(System::IntPtr ptrWindow, System::String ^ text) {

  auto *hwnd = (HWND)ptrWindow.ToPointer();
  DWORD pid;
  DWORD tid;

  GUITHREADINFO info{};
  info.cbSize = sizeof(GUITHREADINFO);

  tid = GetWindowThreadProcessId(hwnd, &pid);

  GetGUIThreadInfo(tid, &info);

  std::string raw_text;
  MarshalString(text, raw_text);

  wchar_t wdata[1024];

  auto size = MultiByteToWideChar(CP_OEMCP, MB_PRECOMPOSED, raw_text.data(),
                                  raw_text.size(), wdata, 0);

  // auto wide_string = std::wstring(raw_text.begin(), raw_text.end());

  // ImmSetCompositionString(0, SCS_SETSTR, pstr, raw_text.size() + 1,
  //                         raw_text.data(), raw_text.size() + 1);

  for (size_t i = 0; i < size; i++) {
    SendMessage(info.hwndFocus, WM_IME_CHAR, wdata[i], 0);
  }

  // SendMessage(info.hwndFocus, WM_IME_COMPOSITION, (WPARAM)pstr,
  // CS_NOMOVECARET);

  // auto handle = GlobalAlloc(0x0002, raw_text.size() + 1);
  // if (!handle)
  //   return false;

  // memcpy(GlobalLock(handle), raw_text.data(), raw_text.size() + 1);

  // GlobalUnlock(handle);

  // if (!OpenClipboard(0)) {
  //   return false;
  // }

  // EmptyClipboard();
  // SetClipboardData(CF_TEXT, handle);
  // if (!CloseClipboard()) {
  //   return false;
  // }

  // handle = NULL;

  // SendMessage(info.hwndFocus, WM_PASTE, 0, 0);

  // if (handle)
  //   GlobalFree(handle);

  return true;
}
} // namespace AskAnywhere
