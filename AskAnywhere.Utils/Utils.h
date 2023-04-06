#pragma once

#include <string>

using namespace System::Runtime::InteropServices;

namespace AskAnywhere {
public
ref class Utils {
public:
  /// <summary>
  /// 获取当前用户输入光标的全局位置
  /// </summary>
  /// <param name="X">[Out] X坐标</param>
  /// <param name="Y">[Out] Y坐标</param>
  /// <returns></returns>
  static System::Boolean
  GetCaretPosition([Out] int % X, [Out] int % Y, [Out] int % Width,
                   [Out] int % Height, [Out] System::IntPtr % ActiveWindow);

  static bool SetActiveWindowAndCaret(System::IntPtr window, int x, int y);

  static bool SendTextToCaret(System::IntPtr ptrWindow, System::String ^ text);
};
} // namespace AskAnywhere
