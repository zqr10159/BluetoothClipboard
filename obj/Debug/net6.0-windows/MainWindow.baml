   M S B A M L   `   `   `  ����M  IBluetoothClipboard, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null7 clr-namespace:BluetoothClipboardBluetoothClipboard  5   	   #    BluetoothClipboard.MainWindow   5   	   W SPresentationCore, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=6.0.2.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=6.0.2.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   _ [MaterialDesignThemes.Wpf, Version=4.8.0.0, Culture=neutral, PublicKeyToken=df2a72020bd7962a:md1http://materialdesigninxaml.net/winfx/xaml/themes  5   	   ,local clr-namespace:BluetoothClipboard  5   	   -   5          � Title$  蓝牙剪贴板��5	   	   $	��450��6   $	��800��6$    � WindowStartupLocation$ CenterScreen=�5
   	   .��5      � $��16q�6    � RowDefinitions 5   
   �� 5      $
��Auto �6   �� 5      $��* �6   �� 5      $
��Auto �6   .+�5   
   � $��0��6   $��0,0,0,16q�6    � ColumnDefinitions 5      �� 5      $��* �6#   �� 5      $
��Auto �6#   �� 5      $
��Auto �6#   �� 5      $
��Auto �6#   �� 5      $
��Auto �6#   .+�5      �� -   5         ��Name$ DevicesComboBox��5      $��0��5      $��0,0,16,0q�5       )  #MaterialDesignThemes.Wpf.HintAssist   Hint$ 选择设备��5!       �� DisplayMemberPath$
 Name��5"      �� 5$      -   5         ��Name$ RefreshButton��5$      $��1��5%          MaterialDesignOutlinedButton#��[  5&      $��刷新设备��5'      $��0,0,16,0q�5(      5)      �� 5+      -   5        $ ServerButton��5+      $��2��5,      #��[  5-      $��启动服务器��5.      $��0,0,16,0q�5/      50      �� 52      -   5        $ ConnectButton��52      $��3��53        MaterialDesignRaisedButton#��[ 54      $��连接��55      $��0,0,16,0q�56      57      �� 59      -   5        $ DisconnectButton��59      $��4��5:      #��[  5;      $��断开连接��5<      5=      5>      �� 5@   
   $��1��6     MaterialDesignBackground#���  5A       �� CornerRadius$ 4��5B      	 �� Padding$	 8q�5C      $��0,0,0,16q�5D      ._�5E      �� -   5        
 ��Name$
 LogScrollViewer��5E      .��5F      �� -   5         ��Name$ 
LogTextBox��5F       �� 
IsReadOnly$
 True��5G       �� TextWrapping$
 Wrap=�5H      " �� VerticalScrollBarVisibility$
 Auto=�5I      
������ $��0q�5K      5L      5M      �� 5O   
   $��2��6   
������ .@�5P      �� -	   5         ��Name$ StatusTextBlock��5P      $��就绪��61   �� 5Q      #��["�6   $
��10,0q�6V   �� 5R      -
   5        $ ConnectionTextBlock��5R      $��	未连接��65   5S      5T      5U      