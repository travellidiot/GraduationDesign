﻿#pragma checksum "..\..\ShowVideo.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "3782175B2F10F3C9579F26DE86BFC734"
//------------------------------------------------------------------------------
// <auto-generated>
//     此代码由工具生成。
//     运行时版本:4.0.30319.34209
//
//     对此文件的更改可能会导致不正确的行为，并且如果
//     重新生成代码，这些更改将会丢失。
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace QueryPro {
    
    
    /// <summary>
    /// ShowVideo
    /// </summary>
    public partial class ShowVideo : System.Windows.Window, System.Windows.Markup.IComponentConnector {
        
        
        #line 14 "..\..\ShowVideo.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.MediaElement myMediaElement;
        
        #line default
        #line hidden
        
        
        #line 17 "..\..\ShowVideo.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Slider timelineSlider;
        
        #line default
        #line hidden
        
        
        #line 19 "..\..\ShowVideo.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.StackPanel appearDock;
        
        #line default
        #line hidden
        
        
        #line 24 "..\..\ShowVideo.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button backButton;
        
        #line default
        #line hidden
        
        
        #line 25 "..\..\ShowVideo.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button playButton;
        
        #line default
        #line hidden
        
        
        #line 26 "..\..\ShowVideo.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button stopBotton;
        
        #line default
        #line hidden
        
        
        #line 27 "..\..\ShowVideo.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button foreBotton;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/QueryPro;component/showvideo.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\ShowVideo.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.myMediaElement = ((System.Windows.Controls.MediaElement)(target));
            
            #line 16 "..\..\ShowVideo.xaml"
            this.myMediaElement.MediaOpened += new System.Windows.RoutedEventHandler(this.myMediaElement_MediaOpened);
            
            #line default
            #line hidden
            
            #line 16 "..\..\ShowVideo.xaml"
            this.myMediaElement.MediaEnded += new System.Windows.RoutedEventHandler(this.myMediaElement_MediaEnded);
            
            #line default
            #line hidden
            
            #line 16 "..\..\ShowVideo.xaml"
            this.myMediaElement.MouseUp += new System.Windows.Input.MouseButtonEventHandler(this.myMediaElement_MouseUp);
            
            #line default
            #line hidden
            return;
            case 2:
            this.timelineSlider = ((System.Windows.Controls.Slider)(target));
            return;
            case 3:
            this.appearDock = ((System.Windows.Controls.StackPanel)(target));
            return;
            case 4:
            this.backButton = ((System.Windows.Controls.Button)(target));
            
            #line 24 "..\..\ShowVideo.xaml"
            this.backButton.Click += new System.Windows.RoutedEventHandler(this.backButton_Click);
            
            #line default
            #line hidden
            return;
            case 5:
            this.playButton = ((System.Windows.Controls.Button)(target));
            
            #line 25 "..\..\ShowVideo.xaml"
            this.playButton.Click += new System.Windows.RoutedEventHandler(this.playButton_Click);
            
            #line default
            #line hidden
            return;
            case 6:
            this.stopBotton = ((System.Windows.Controls.Button)(target));
            
            #line 26 "..\..\ShowVideo.xaml"
            this.stopBotton.Click += new System.Windows.RoutedEventHandler(this.stopBotton_Click);
            
            #line default
            #line hidden
            return;
            case 7:
            this.foreBotton = ((System.Windows.Controls.Button)(target));
            
            #line 27 "..\..\ShowVideo.xaml"
            this.foreBotton.Click += new System.Windows.RoutedEventHandler(this.foreBotton_Click);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}
