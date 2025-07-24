# VaM Resource Manager

**Author:** sethi_dash  
**License:** GNU General Public License v3.0  

---

## Overview

**VaM Resource Manager (VRM)** is a fast, standalone WPF-based resource browser and management tool for [Virt-A-Mate (VaM)](https://www.patreon.com/meshedvr).  
It helps optimize loading times and improve workflow by allowing you to control exactly which resources get loaded into VaM.

VRM does **not** modify `.var` files — it only moves or references them to reduce clutter and speed up startup and browsing.

---

## Requirements

- **.NET Framework 4.8**  
  Make sure it's installed on your system before launching VRM.  
  You can download it from: https://dotnet.microsoft.com/en-us/download/dotnet-framework

---

## Key Features

- Visual content browser for `.var` and user-created content
- Clean separation of:
  - **Loaded** – items that VaM will load
  - **Archived** – items that are ignored by VaM
  - **Referenced** – your custom lists of required content
- Powerful filtering and tagging tools
- Completely open-source and free

---

## Tabs Overview

- **var**: Displays all `.var` files and their internal content
- **user data**: Content not in `.var` files (custom scenes, audio, scripts, etc.)
- **vam**: Shows global stats and settings related to VaM
- **log**: Displays recent events and actions

Each tab includes its own set of filters and actions.

---

## Basic Workflow

1. Browse and filter content visually
2. Mark desired items by adding them to a Reference
3. Use **Sync** to apply changes:
   - Referenced items are moved to **Loaded**
   - All others are moved to **Archived**

---

## License

This project is licensed under the [GNU GPL v3](https://www.gnu.org/licenses/gpl-3.0.en.html).  

## Third-party modules used

This project uses the following third-party components:
- Costura.Fody (v6.0.0) — MIT License: https://opensource.org/licenses/MIT
- Fody (v6.8.2) — MIT License: https://opensource.org/licenses/MIT
- Microsoft.Xaml.Behaviors.Wpf (v1.1.135) — MIT License: https://opensource.org/licenses/MIT
- Newtonsoft.Json (v13.0.3) — MIT License: https://opensource.org/licenses/MIT
- VirtualizingWrapPanel (v2.1.2) — MIT License: https://opensource.org/licenses/MIT

The authors of these libraries retain all rights to their respective code.
Please refer to their respective licenses for more information.

## Contact

GitHub project: [https://github.com/sethi-dash/VamResourceManager](https://github.com/sethi-dash/VamResourceManager)  
Created by **sethi_dash@outlook.com**
