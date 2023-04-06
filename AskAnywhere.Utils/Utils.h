#pragma once

#include <string>

using namespace System::Runtime::InteropServices;

namespace AskAnywhere {
public
ref class Utils {
public:
  /// <summary>
  /// ��ȡ��ǰ�û��������ȫ��λ��
  /// </summary>
  /// <param name="X">[Out] X����</param>
  /// <param name="Y">[Out] Y����</param>
  /// <returns></returns>
  static System::Boolean
  GetCaretPosition([Out] int % X, [Out] int % Y, [Out] int % Width,
                   [Out] int % Height, [Out] System::IntPtr % ActiveWindow);

  static bool SetActiveWindowAndCaret(System::IntPtr window, int x, int y);

  static bool SendTextToCaret(System::IntPtr ptrWindow, System::String ^ text);
};
} // namespace AskAnywhere
