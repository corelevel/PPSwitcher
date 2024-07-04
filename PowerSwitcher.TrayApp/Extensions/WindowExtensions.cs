﻿using PowerSwitcher.TrayApp.Services;
using System;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PowerSwitcher.TrayApp.Extensions
{
    ////
    //  Code heavily inspired by https://github.com/File-New-Project/EarTrumpet/blob/master/EarTrumpet/Extensions/WindowExtensions.cs
    ////

    [SupportedOSPlatform("windows")]
    internal static class WindowExtensions
    {
        private static bool hideAnimationInProgress = false;
        private static bool showAnimationInProgress = false;

        public static void HideWithAnimation(this Window window)
        {
            if (hideAnimationInProgress) return;

            try
            {
                hideAnimationInProgress = true;

                var hideAnimation = new DoubleAnimation
                {
                    Duration = new Duration(TimeSpan.FromSeconds(0.2)),
                    FillBehavior = FillBehavior.Stop,
                    EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseIn }
                };
                var taskbarPosition = TaskbarService.GetWinTaskbarState().TaskbarPosition;
                switch (taskbarPosition)
                {
                    case TaskbarPosition.Left:
                    case TaskbarPosition.Right:
                        hideAnimation.From = window.Left;
                        break;
                    default:
                        hideAnimation.From = window.Top;
                        break;
                }
                hideAnimation.To = (taskbarPosition == TaskbarPosition.Top || taskbarPosition == TaskbarPosition.Left) ? hideAnimation.From - 30 : hideAnimation.From + 30;
                hideAnimation.Completed += (s, e) =>
                {
                    window.Visibility = Visibility.Hidden;
                    hideAnimationInProgress = false;
                };

                switch (taskbarPosition)
                {
                    case TaskbarPosition.Left:
                    case TaskbarPosition.Right:
                        window.ApplyAnimationClock(Window.LeftProperty, hideAnimation.CreateClock());
                        break;
                    default:
                        window.ApplyAnimationClock(Window.TopProperty, hideAnimation.CreateClock());
                        break;
                }
            }
            catch
            {
                hideAnimationInProgress = false;
            }
        }

        public static void ShowWithAnimation(this Window window)
        {
            if (showAnimationInProgress) return;

            try
            {
                showAnimationInProgress = true;
                window.Visibility = Visibility.Visible;
                window.Topmost = false;
                window.Activate();
                var showAnimation = new DoubleAnimation
                {
                    Duration = new Duration(TimeSpan.FromSeconds(0.3)),
                    FillBehavior = FillBehavior.Stop,
                    EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut }
                };
                var taskbarPosition = TaskbarService.GetWinTaskbarState().TaskbarPosition;
                switch (taskbarPosition)
                {
                    case TaskbarPosition.Left:
                    case TaskbarPosition.Right:
                        showAnimation.To = window.Left;
                        break;
                    default:
                        showAnimation.To = window.Top;
                        break;
                }
                showAnimation.From = (taskbarPosition == TaskbarPosition.Top || taskbarPosition == TaskbarPosition.Left) ? showAnimation.To - 60 : showAnimation.To + 60;
                showAnimation.Completed += (s, e) =>
                {
                    window.Topmost = true;
                    showAnimationInProgress = false;
                    window.Focus();
                };
                switch (taskbarPosition)
                {
                    case TaskbarPosition.Left:
                    case TaskbarPosition.Right:
                        window.ApplyAnimationClock(Window.LeftProperty, showAnimation.CreateClock());
                        break;
                    default:
                        window.ApplyAnimationClock(Window.TopProperty, showAnimation.CreateClock());
                        break;
                }
            }
            catch
            {
                showAnimationInProgress = false;
            }
        }

        public static Matrix CalculateDpiFactors(this Window window)
        {
            var mainWindowPresentationSource = PresentationSource.FromVisual(window);
            return mainWindowPresentationSource == null ? new Matrix() { M11 = 1, M22 = 1 } : mainWindowPresentationSource.CompositionTarget.TransformToDevice;
        }

        public static double DpiHeightFactor(this Window window)
        {
            var m = CalculateDpiFactors(window);
            return m.M22;
        }

        public static double DpiWidthFactor(this Window window)
        {
            var m = CalculateDpiFactors(window);
            return m.M11;
        }
    }
}
