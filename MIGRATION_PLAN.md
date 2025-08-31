# WMO Asset Patcher - UI Migration Plan

## 📋 **Step-by-Step Migration Guide**

### **Phase 1: Project Setup & Dependencies**

#### 1.1 Update Project File
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>  <!-- Changed from Exe to WinExe -->
    <UseWPF>true</UseWPF>            <!-- Enable WPF -->
    <TargetFramework>net9.0-windows</TargetFramework>  <!-- Windows-specific -->
    <!-- ... existing properties ... -->
  </PropertyGroup>
  
  <!-- Add WPF-specific packages -->
  <ItemGroup>
    <PackageReference Include="Microsoft.Xaml.Behaviors.Wpf" Version="1.1.77" />
    <PackageReference Include="MaterialDesignThemes" Version="5.1.0" />  <!-- Optional: Modern UI -->
    <!-- ... existing packages ... -->
  </ItemGroup>
</Project>
```

#### 1.2 New Folder Structure
```
src/
├── Program.cs                    (Modified - WPF entry point)
├── UI/
│   ├── MainWindow.xaml          (Main application window)
│   ├── MainWindow.xaml.cs       (Code-behind)
│   ├── Views/
│   │   ├── PatchingView.xaml    (Patching feature)
│   │   ├── ModManagement.xaml   (Mod browsing/management)
│   │   ├── SettingsView.xaml    (Configuration)
│   │   └── LogView.xaml         (Real-time logging)
│   ├── ViewModels/
│   │   ├── MainViewModel.cs     (Main window logic)
│   │   ├── PatchingViewModel.cs (Patching operations)
│   │   ├── ModViewModel.cs      (Mod management)
│   │   └── SettingsViewModel.cs (Settings management)
│   ├── Controls/
│   │   ├── ModCard.xaml         (Custom mod display control)
│   │   ├── ProgressCard.xaml    (Progress display)
│   │   └── LogOutput.xaml       (Styled log display)
│   └── Resources/
│       ├── Styles.xaml          (Application styles)
│       └── Icons/               (UI icons)
├── Core/                        (Renamed from existing folders)
│   ├── Services/
│   │   ├── PatchingService.cs   (Async patching operations)
│   │   ├── ModService.cs        (Mod discovery/management)
│   │   └── UILoggerService.cs   (UI-aware logging)
│   ├── Models/
│   │   ├── ModInfo.cs           (Enhanced mod metadata)
│   │   ├── PatchOperation.cs    (Progress tracking)
│   │   └── AppSettings.cs       (UI settings)
│   └── ... (existing Helper, Logger, Patcher folders)
```

### **Phase 2: Core Architecture Changes**

#### 2.1 New Program.cs (WPF Entry Point)
```csharp
using System.Windows;
using WMO.UI;

namespace WMO;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var app = new Application();
        var mainWindow = new MainWindow();
        
        // Handle command line arguments
        if (args.Contains("--console"))
        {
            // Optional: Keep console mode for CI/automated scenarios
            RunConsoleMode(args);
            return;
        }
        
        app.Run(mainWindow);
    }
    
    private static void RunConsoleMode(string[] args)
    {
        // Keep existing console logic for backwards compatibility
        // ... existing console code ...
    }
}
```

#### 2.2 Async Service Layer
```csharp
public class PatchingService
{
    public event EventHandler<PatchProgressEventArgs> ProgressChanged;
    public event EventHandler<string> LogReceived;
    
    public async Task<bool> PatchAssetsAsync(string gamePath, 
        IProgress<PatchProgress> progress, 
        CancellationToken cancellationToken)
    {
        // Convert existing synchronous patching to async
        // Report progress for UI updates
        // Handle cancellation requests
    }
}
```

### **Phase 3: UI Features Design**

#### 3.1 Main Window Layout
```
┌─────────────────────────────────────────────────────────┐
│ WMO Asset Patcher                              [─][□][×] │
├─────────────────────────────────────────────────────────┤
│ [Patching] [Mods] [Settings] [Logs]                     │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌─ Game Path ──────────────────────────────────────┐   │
│  │ C:\...\Whisper Mountain Outbreak_Data  [Browse] │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  ┌─ Available Mods ────────────────────────────────┐   │
│  │ ☑ Audio Pack (38 files)                        │   │
│  │ ☐ Custom Sprites (12 files)                    │   │
│  │ ☐ UI Improvements (5 files)                    │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  [📁 Install Mods] [🔄 Patch Game] [⏹ Stop] [🧹 Clean] │
│                                                         │
│  ┌─ Progress ──────────────────────────────────────┐   │
│  │ Patching sharedassets3.assets... [████████░░] 80% │   │
│  └─────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
```

#### 3.2 Enhanced Features to Add
1. **Mod Management**
   - Drag & drop mod installation
   - Mod browsing with previews
   - Enable/disable individual mods
   - Mod dependency checking

2. **Advanced Patching**
   - Selective patching (choose which mods to apply)
   - Batch operations
   - Rollback/restore functionality
   - Patch verification

3. **User Experience**
   - Real-time progress bars
   - Background operations
   - System tray minimization
   - Auto-updater integration

4. **Developer Features**
   - Mod creation wizard
   - Asset preview/editing
   - Debug console integration
   - Performance profiling

### **Phase 4: Data Binding & MVVM**

#### 4.1 ViewModel Example
```csharp
public class PatchingViewModel : INotifyPropertyChanged
{
    private string _gamePath;
    private ObservableCollection<ModInfo> _availableMods;
    private bool _isPatchingInProgress;
    private double _patchingProgress;
    
    public ICommand BrowseGamePathCommand { get; }
    public ICommand StartPatchingCommand { get; }
    public ICommand StopPatchingCommand { get; }
    
    // Property change notifications for UI binding
    public string GamePath
    {
        get => _gamePath;
        set => SetProperty(ref _gamePath, value);
    }
    
    public async Task StartPatchingAsync()
    {
        IsPatchingInProgress = true;
        try
        {
            var progress = new Progress<PatchProgress>(p => 
            {
                PatchingProgress = p.Percentage;
                StatusMessage = p.Message;
            });
            
            await _patchingService.PatchAssetsAsync(GamePath, progress, _cancellationToken);
        }
        finally
        {
            IsPatchingInProgress = false;
        }
    }
}
```

### **Phase 5: Migration Strategy**

#### 5.1 Gradual Migration Approach
1. **Keep console mode** as fallback (`--console` flag)
2. **Extract business logic** into services first
3. **Build UI incrementally** (start with basic patching)
4. **Add features progressively** (mods → settings → advanced)

#### 5.2 Code Reuse Strategy
```csharp
// Extract existing logic into reusable services
public static class ConsoleToUIBridge
{
    public static async Task<bool> RunPatchingWithUI(
        string gamePath, 
        IProgress<PatchProgress> progress = null)
    {
        // Wrap existing AssetPatcher logic
        // Add progress reporting
        // Make async-compatible
    }
}
```

### **Phase 6: Implementation Priorities**

#### Priority 1: Core Migration (Week 1-2)
- [ ] Update project configuration for WPF
- [ ] Create basic MainWindow with tabbed interface
- [ ] Migrate patching logic to async service
- [ ] Implement basic progress reporting

#### Priority 2: Essential Features (Week 3-4)
- [ ] Game path selection with file browser
- [ ] Mod discovery and display
- [ ] Real-time logging output
- [ ] Basic error handling and recovery

#### Priority 3: Enhanced UX (Week 5-6)
- [ ] Drag & drop mod installation
- [ ] Selective mod patching
- [ ] Settings persistence
- [ ] Backup management UI

#### Priority 4: Advanced Features (Week 7+)
- [ ] Mod creation tools
- [ ] Auto-updater integration
- [ ] Plugin system for extensibility
- [ ] Multi-game support

## 🛠 **Technical Considerations**

### **Threading**
- UI thread for interface updates
- Background threads for file operations
- Progress reporting via `IProgress<T>`
- Cancellation support via `CancellationToken`

### **Error Handling**
- Global exception handlers for UI
- User-friendly error dialogs
- Detailed logging for debugging
- Graceful degradation

### **Performance**
- Lazy loading of mod lists
- Virtual scrolling for large mod collections
- Background mod scanning
- Memory-efficient asset handling

### **Accessibility**
- Keyboard navigation support
- Screen reader compatibility
- High contrast mode support
- Scalable UI elements

This migration will transform your console patcher into a professional mod loader while preserving all existing functionality!
