﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ItemsControlExtensions.cs" company="Orcomp development team">
//   Copyright (c) 2008 - 2014 Orcomp development team. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace LogViewer.Extensions
{
    using System.Collections.Generic;
    using System.Windows.Controls;

    public static class ItemsControlExtensions
    {
        public static IEnumerable<T> EnumerateNested<T>(this ItemsControl rootControl) where T : ItemsControl
        {
            var stack = new Queue<ItemsControl>();
            stack.Enqueue(rootControl);

            while (stack.Count != 0)
            {
                var item = stack.Dequeue();

                for (int i = 0; i < item.Items.Count; i++)
                {
                    var subItem = item.ItemContainerGenerator.ContainerFromIndex(i) as ItemsControl;
                    if (subItem != null)
                    {
                        stack.Enqueue(subItem);
                    }

                    var targetTyped = subItem as T;
                    if (targetTyped != null)
                    {
                        yield return targetTyped;
                    }
                }
            }
        }
    }
}