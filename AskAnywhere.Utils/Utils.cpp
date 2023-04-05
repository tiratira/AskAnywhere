#include "pch.h"
#include "Utils.h"
#include "oleacc.h"

#pragma comment(lib, "Oleacc.lib")

namespace AskAnywhere {
	System::Boolean Utils::GetCaretPosition(int% X, int% Y, int% Width,
		int% Height,
		System::IntPtr% ActiveWindow) {
		HWND hwnd;
		DWORD pid;
		DWORD tid;

		if (!SUCCEEDED(CoInitialize(nullptr))) return false;

		GUITHREADINFO info{};
		info.cbSize = sizeof(GUITHREADINFO);

		hwnd = GetForegroundWindow();
		tid = GetWindowThreadProcessId(hwnd, &pid);

		GetGUIThreadInfo(tid, &info);

		IAccessible* object = nullptr;

		if (!SUCCEEDED(AccessibleObjectFromWindow(
			info.hwndFocus, OBJID_CARET, IID_IAccessible, (void**)&object))) {
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
		CoUninitialize();
		return true;
	}

	bool Utils::SetActiveWindowAndCaret(System::IntPtr window, int x, int y) {
		if (!SUCCEEDED(CoInitialize(nullptr))) return false;

		auto hwnd = (HWND)window.ToPointer();

		SetForegroundWindow(hwnd);

		SetCaretPos(x, y);

		CoUninitialize();

		return true;
	}
}  // namespace AskAnywhere
