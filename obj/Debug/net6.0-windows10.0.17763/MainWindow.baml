   M S B A M L   `   `   `  ����  BluetoothClipboard*    $BluetoothClipboardSharing.MainWindow   5   	   W SPresentationCore, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=6.0.2.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=6.0.2.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	     � Title$  蓝牙剪贴板共享��5   	   $	��500��6   $	��800��6&   .��5      �  � RowDefinitions 5   
   �� 5      $
��Auto �6   �� 5      $��* �6   �� 5	      $
��Auto �6   .+�5   
   ��  �� Orientation$ 
Horizontal=�6   $��5q�6.   .+�5      �� -   5         ��Name$ DevicesComboBox��5      $	��200��60   $��5q�6<   �� 5      -   5        $��搜索设备��5      $��5q�6@   �� 5      -   5         ��Name$ ConnectButton��5      $��连接��6,   $��5q�6O   �� 5      -   5        $��断开��5      $��5q�6;   5      �� 5   
   -   5         ��Name$ LogBox��5      $��1��6"   $��5q�6/    �� 
IsReadOnly$
 True��5      " �� VerticalScrollBarVisibility$
 Auto=�6$   $��Consolas)�5      $��12(�6(   ���#1E1E1E
�������5   '   �� 5   
   $��2��6   .@�5      �� $��	状态：��6   $
��Bold"�6*   �� 5      -   5         ��Name$ StatusLabel��5      $��	未连接��61   5      5      5      