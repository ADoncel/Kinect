   M S B A M L   `   `   `  ����  SkeletonBasics-WPF8    2Microsoft.Samples.Kinect.SkeletonBasics.MainWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   -   5          � Title$  Skeleton Basics��5   	   $	��735��6!   $	��770��6.    � 	Resources 5         MediumGreyBrush �� Color  KinectPurpleBrush  KinectBlueBrush �� 
TargetType �� Property �� Value  SquareCheckBox �� 
TargetType �� Orientation �Name	 ��Name
 ��Name �� TextAlignment �� Triggers P� Property P� Value �� 
TargetName��%1  &	        &	 #     &	 F     '�� i     &	 �     7  7  �� 5   
   $ 	#ff6e6e6e��62   �� 5   
   $ 	#ff52318f��64   �� 5	   
   $ 	#ff00BCF2��62   �� 5
   
   " ��6   .�5      ��  �@J�SnapsToDevicePixels . 64   5      �� 5   
   " ��6   .�5      ��  � �� 5      �� 5      " ��6&   .R�5      � .+�5      �� $ 
Horizontal=�6)   
������ .+�5   "   � $ SquareCheckBoxChecked��6'   .+�5   &   �� $	 CheckedNormal��6,   $��Images\CheckedNormal.png��6C   $
��None=�6e   $��Center=�6t   �� 5   &   $	 CheckedHover��6,   $��Images\CheckedHover.png��6B   $
��None=�6c   $��Center=�6r   $y�	Collapsed=�6�   5   #   � 5   "   $ SquareCheckBoxUnchecked��6'   $y�	Collapsed=�6H   .+�5   &   �� $	 UncheckedNormal��6,   $ ��Images\UncheckedNormal.png��6E   $
��None=�6i   $��Center=�6x   �� 5   &   $	 UncheckedHover��6,   $��Images\UncheckedHover.png��6D   $
��None=�6g   $��Center=�6v   $y�	Collapsed=�6�   5   #   �� 5   "   $
 SquareCheckBoxText��6,   #��z��6H   $
 Left=�6i   $��Center=�6   8��  $��15(�6�   $��9,0,0,0q�6�   5      5       5      P� 5       �@d�	IsChecked$ falseY�6;   . �5    "   �� $ SquareCheckBoxChecked��6Q    � y�$ 	Collapsed=�6?   6Q   �� 5!   "   $ SquareCheckBoxUnchecked��6O    � y�$ Visible=�6?   6O   5"      P� 5#       �@J�IsMouseOver . . �5$   "   �� $ CheckedNormal��6Q    � y�$ 	Collapsed=�6?   6Q   �� 5%   "   $ UncheckedNormal��6Q    � y�$ 	Collapsed=�6?   6Q   �� 5&   "   $ CheckedHover��6O    � y�$ Visible=�6?   6O   �� 5'   "   $ UncheckedHover��6O    � y�$ Visible=�6?   6O   �� 5(   "   8  $ SquareCheckBoxText��6h    � ��5)      5+      5-      5.      5/      .��50      � -   5        $��
layoutGrid��50      $��	10 0 10 0q�6    � RowDefinitions 51   
   �� 52      $��92 �6   �� 53      $
��530* �6   �� 54      $��0 �6   �� 55      $��74 �6   .+�57   
   ]� $��0��6   $��0 0 0 20q�6!   .+�58      �� -   5         ��Name$ 	solucionP��58      $$��Establezca la posicion inicial��6'   $��20(�6P   $	��725��6^   $	��102��6j   $��Center=�6w   $	��Top=�6�   
����  �$��Center=�6�   $
��Bold"�6�   $��Normal$�6�   $��	Yu Gothic)�6  $��Normal&�6  66  59      (� 5;   
   $��1��6    (� Stretch$ Uniform=�6   $��Center=�61   .��5<      � .+�5=      �� -   5        $��
ColorImage��5=      �� 5>      -   5        $��Image��5>      �� 5?      
������ 64   5@      5A      �� 5B   
   -   5        $��	statusBar��5B   ?   $��3��6   $��Stretch=�6!   $��Bottom=�6P   
�������#��[  6~   .@�5C      �� $��0 0 0 10q�6   61   5D      (� 5E   
   $��3��6   $ Uniform=�6   $��Center=�61   .��5F      � .+�5G      �� -   5        $��button1��5G   g   $��	Reiniciar��6   $��36��6-   $
��Left=�69   $��	23,15,0,0q�6T   $	��Top=�6v   $	��107��6�   6�   �� 5H      -   5        $��slider1��5H   T   $��19��6   $
��Left=�6%   $��
194,22,0,0q�6@   $	��Top=�6c   $	��139��6{    � Maximum$ 15Q�6�    � Minimum$ 0Q�6�    � Value$ 0Q�6�   �� 5I      -	   5        $��
textBlock1��5I   =   $��23��6   $��194,1,395,0q�6(   $��               % Error��6O   $	��Top=�6m   �� 5J      -
   5        $��
textBlock2��5J   W   $��19��6   $
��Left=�6(   $��
170,22,0,0q�6C   $��5%��6i   $	��Top=�6s   $��18��6�   �� 5K      -   5        $��
textBlock3��5K   >   $��19��6   $��338,0,366,33q�6(   $	��15%��6P   $��Bottom=�6[   �� 5L      -   5        $��
textBlock4��5L   W   $��23��6   $
��Left=�6(   $��
240,41,0,0q�6C   $�� ��6i   $	��Top=�6q   �� 5M      -   5        $��muestraError��5M   W   $��23��6   $
��Left=�6(   $��
260,41,0,0q�6C   $��5%��6k   $	��Top=�6u   �� 5N      -   5        $��repeticiones��5N   T   $��23��6   $
��Left=�6&   $��	505,1,0,0q�6A   $	��Top=�6h   $��76��6�   $��1��6�   $��Right=�6�   $��Center=�6�    � DataContext 6�   ��  �� ElementName$ repeticiones��6�   �� 5O      -   5        $��button2��5O   i   $��
Establecer��6   $��23��6.   $
��Left=�6:   $��
505,32,0,0q�6U   $	��Top=�6x   $��75��6�   6�   �� 5P      -   5        $��
textBlock5��5P   W   $��23��6   $
��Left=�6(   $��
412,18,0,0q�6C   $��Nº Repeticiones:��6i   $	��Top=�6�   �� 5Q      -   5        $��
textBlock6��5Q   W   $��23��6   $
��Left=�6(   $��
598,18,0,0q�6C   $��
Restantes:��6i   $	��Top=�6{   �� 5R      -   5        $��movRestantes��5R   W   $��23��6   $
��Left=�6(   $��
659,18,0,0q�6C   $��0��6k   $	��Top=�6t   5S      5T      5U      5V      