﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#nullable enable
namespace Splatoon.SplatoonScripting
{
    public class Controller
    {
        internal Dictionary<string, Layout> Layouts = new();
        internal Dictionary<string, Element> Elements = new();

        internal int autoIncrement = 0;
        internal int AutoIncrement => ++autoIncrement;

        /// <summary>
        /// Attempts to register previously exported from plugin layout for further usage. End user will be able to edit this layout as they wish and results of the edit will be saved. Enabled layouts are subject for immediate processing when the script is enabled.
        /// </summary>
        /// <param name="UniqueName">Internal unique (within current script) name of the layout.</param>
        /// <param name="ExportString">An exported layout string.</param>
        /// <param name="layout">Decoded layout object.</param>
        /// <param name="overwrite">Whether to overwrite existing layout with same name if it's present.</param>
        /// <returns>Whether layout was successfully registered.</returns>
        public bool TryRegisterLayoutFromCode(string UniqueName, string ExportString, [NotNullWhen(true)] out Layout? layout, bool overwrite = false)
        {
            return ScriptingEngine.TryDecodeLayout(ExportString, out layout) && TryRegisterLayout(UniqueName, layout, overwrite);
        }

        public bool TryRegisterLayoutFromCode(string ExportString, [NotNullWhen(true)] out Layout? layout, bool overwrite = false)
        {
            return TryRegisterLayoutFromCode($"unnamed-{AutoIncrement}", ExportString, out layout, overwrite);
        }

        /// <summary>
        /// Attempts to register previously constructed layout for further usage. End user will be able to edit this layout as they wish and results of the edit will be saved. Enabled layouts are subject for immediate processing when the script is enabled.
        /// </summary>
        /// <param name="UniqueName">Internal unique (within current script) name of the layout.</param>
        /// <param name="layout">Layout object.</param>
        /// <param name="overwrite">Whether to overwrite existing layout with same name if it's present.</param>
        /// <returns>Whether layout was successfully registered.</returns>
        public bool TryRegisterLayout(string UniqueName, Layout layout, bool overwrite = false)
        {
            if (!overwrite && Layouts.ContainsKey(UniqueName))
            {
                PluginLog.Warning($"There is a layout named {UniqueName} already.");
                return false;
            }
            Layouts[UniqueName] = layout;
            return true;
        }


        public bool TryRegisterLayout(Layout layout, bool overwrite = false)
        {
            return TryRegisterLayout($"unnamed-{AutoIncrement}", layout, overwrite);
        }

        /// <summary>
        /// Attempts to register previously constructed element for further usage. End user will be able to edit this element as they wish and results of the edit will be saved. Enabled elements are subject for immediate processing when the script is enabled.
        /// </summary>
        /// <param name="UniqueName">Internal unique (within current script) name of the element.</param>
        /// <param name="element">Element object.</param>
        /// <param name="overwrite">Whether to overwrite existing element with same name if it's present.</param>
        /// <returns>Whether element was successfully registered.</returns>
        public bool TryRegisterElement(string UniqueName, Element element, bool overwrite = false)
        {
            if (!overwrite && Layouts.ContainsKey(UniqueName))
            {
                PluginLog.Warning($"There is an element named {UniqueName} already.");
                return false;
            }
            Elements[UniqueName] = element;
            return true;
        }

        /// <summary>
        /// Attempts to register previously exported from plugin element for further usage. End user will be able to edit this element as they wish and results of the edit will be saved. Enabled elements are subject for immediate processing when the script is enabled.
        /// </summary>
        /// <param name="UniqueName">Internal unique (within current script) name of the element</param>
        /// <param name="ExportString">An exported element string.</param>
        /// <param name="element">Decoded element object.</param>
        /// <param name="overwrite">Whether to overwrite existing element with same name if it's present.</param>
        /// <returns>Whether element was successfully registered.</returns>
        public bool TryRegisterElementFromCode(string UniqueName, string ExportString, [NotNullWhen(true)] out Element? element, bool overwrite = false)
        {
            return ScriptingEngine.TryDecodeElement(ExportString, out element) && TryRegisterElement(UniqueName, element, overwrite);
        }

        /// <summary>
        /// Tries to get previously registered layout by name.
        /// </summary>
        /// <param name="name">Layout's internal name.</param>
        /// <param name="layout">Result.</param>
        /// <returns>Whether operation succeeded.</returns>
        public bool TryGetLayoutByName(string name, [NotNullWhen(true)] out Layout? layout)
        {
            return Layouts.TryGetValue(name, out layout);
        }

        /// <summary>
        /// Tries to get previously registered element by name.
        /// </summary>
        /// <param name="name">Element's internal name.</param>
        /// <param name="element">Result.</param>
        /// <returns>Whether operation succeeded.</returns>
        public bool TryGetElementByName(string name, [NotNullWhen(true)] out Element? element)
        {
            return Elements.TryGetValue(name, out element);
        }

        /// <summary>
        /// Unregisters previously registered layout.
        /// </summary>
        /// <param name="name">Layout name.</param>
        /// <returns>Whether operation succeeded.</returns>
        public bool TryUnregisterLayout(string name)
        {
            return Layouts.Remove(name);
        }

        /// <summary>
        /// Unregisters previously registered element.
        /// </summary>
        /// <param name="name">Element name.</param>
        /// <returns>Whether operation succeeded.</returns>
        public bool TryUnregisterElement(string name)
        {
            return Elements.Remove(name);
        }

        /// <summary>
        /// Returns a dictionary of currently registered layouts.
        /// </summary>
        /// <returns>Read only dictionary of currently registered layouts.</returns>
        public ReadOnlyDictionary<string, Layout> GetRegisteredLayouts()
        {
            return new ReadOnlyDictionary<string, Layout>(Layouts);
        }

        /// <summary>
        /// Returns a dictionary of currently registered elements.
        /// </summary>
        /// <returns>Read only dictionary of currently registered elements.</returns>
        public ReadOnlyDictionary<string, Element> GetRegisteredElements()
        {
            return new ReadOnlyDictionary<string, Element>(Elements);
        }

        /// <summary>
        /// Removes all layouts.
        /// </summary>
        public void ClearRegisteredLayouts()
        {
            Layouts.Clear();
        }

        /// <summary>
        /// Removes all elements
        /// </summary>
        public void ClearRegisteredElements()
        {
            Elements.Clear();
        }

        /// <summary>
        /// Removes all elements and layouts
        /// </summary>
        public void Clear()
        {
            ClearRegisteredElements();
            ClearRegisteredLayouts();
        }
    }
}