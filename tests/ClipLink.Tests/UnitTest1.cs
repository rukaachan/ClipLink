using ClipLink.Core;
using ClipLink.Worker;

namespace ClipLink.Tests
{
    public sealed class BridgeConfigurationTests
    {
        [Fact]
        public void ExpandPath_ExpandsEnvironmentVariables()
        {
            var expected = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Pictures",
                "ClipLink");

            var expanded = BridgeConfiguration.ExpandPath(@"%USERPROFILE%\Pictures\ClipLink");

            Assert.Equal(expected, expanded);
        }

        [Fact]
        public void ResolveStorageDirectory_UsesRootFolderDirectly()
        {
            var config = new BridgeConfiguration
            {
                ImageRootDirectory = @"C:\Users\example\Pictures\ClipLink"
            };

            var result = config.ResolveStorageDirectory(new DateTime(2026, 4, 5, 9, 30, 0));

            Assert.Equal(@"C:\Users\example\Pictures\ClipLink", result);
        }

        [Fact]
        public void BuildPrompt_IncludesSavedImagePath()
        {
            var config = new BridgeConfiguration
            {
                PromptTemplate = " : {path}"
            };

            var prompt = config.BuildPrompt(@"C:\Users\example\Pictures\ClipLink\img-1.png");

            Assert.Equal(@" : C:\Users\example\Pictures\ClipLink\img-1.png", prompt);
        }

        [Fact]
        public void BuildPrompt_ThrowsWhenTemplateOmitsPathPlaceholder()
        {
            var config = new BridgeConfiguration
            {
                PromptTemplate = "analyze this image"
            };

            Assert.Throws<InvalidOperationException>(() => config.BuildPrompt(@"C:\img.png"));
        }
    }

    public sealed class FileRetentionServiceTests
    {
        [Fact]
        public void GetExpiredFiles_ReturnsOnlyFilesOlderThanRetentionWindow()
        {
            var root = Path.Combine(Path.GetTempPath(), $"bridge-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);

            try
            {
                var keep = Path.Combine(root, "img-new.png");
                var expired = Path.Combine(root, "img-old.png");
                var nestedDirectory = Directory.CreateDirectory(Path.Combine(root, "2026-04-05"));
                File.WriteAllText(keep, "new");
                File.WriteAllText(expired, "old");
                File.SetLastWriteTimeUtc(keep, new DateTime(2026, 4, 5, 8, 30, 0, DateTimeKind.Utc));
                File.SetLastWriteTimeUtc(expired, new DateTime(2026, 4, 5, 7, 29, 59, DateTimeKind.Utc));

                var files = FileRetentionService.GetExpiredFiles(
                    root,
                    new DateTime(2026, 4, 5, 8, 30, 0, DateTimeKind.Utc),
                    TimeSpan.FromHours(1));

                Assert.Equal([expired], files);
                Assert.DoesNotContain(keep, files);
                Assert.DoesNotContain(nestedDirectory.FullName, files);
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    public sealed class StartupRegistrationFormatterTests
    {
        [Fact]
        public void BuildRunCommand_QuotesExecutablePath()
        {
            var command = StartupRegistrationFormatter.BuildRunCommand(
                @"C:\Program Files\ClipLink\ClipLink.exe");

            Assert.Equal(
                "\"C:\\Program Files\\ClipLink\\ClipLink.exe\"",
                command);
        }

        [Fact]
        public void BuildRunCommand_ThrowsForBlankPath()
        {
            Assert.Throws<ArgumentException>(() => StartupRegistrationFormatter.BuildRunCommand(" "));
        }
    }

    public sealed class PasteInjectionSettingsTests
    {
        [Fact]
        public void CreateDefault_UsesTerminalFriendlyPasteShortcut()
        {
            var settings = PasteInjectionSettings.CreateDefault();

            Assert.Equal("+{INSERT}", settings.SendKeysSequence);
            Assert.Equal(750, settings.RestoreDelayMilliseconds);
            Assert.Equal(1500, settings.TerminalRestoreDelayMilliseconds);
            Assert.False(settings.RestoreClipboardAfterPaste);
        }

        [Theory]
        [InlineData("alacritty", "^+v")]
        [InlineData("WindowsTerminal", "^+v")]
        [InlineData("OpenConsole", "+{INSERT}")]
        [InlineData("pwsh", "+{INSERT}")]
        [InlineData("chrome", "^v")]
        [InlineData("msedge", "^v")]
        [InlineData("firefox", "^v")]
        public void ResolveSequence_UsesProcessSpecificPasteShortcut(string processName, string expectedSequence)
        {
            var settings = PasteInjectionSettings.CreateDefault();

            var sequence = settings.ResolveSequence(processName);

            Assert.Equal(expectedSequence, sequence);
        }

        [Theory]
        [InlineData("alacritty", 1500)]
        [InlineData("WindowsTerminal", 1500)]
        [InlineData("OpenConsole", 750)]
        [InlineData("pwsh", 750)]
        [InlineData(null, 750)]
        public void ResolveRestoreDelayMilliseconds_UsesProcessSpecificDelay(string? processName, int expectedDelay)
        {
            var settings = PasteInjectionSettings.CreateDefault();

            var delay = settings.ResolveRestoreDelayMilliseconds(processName);

            Assert.Equal(expectedDelay, delay);
        }
    }

    public sealed class SingleInstancePolicyTests
    {
        [Fact]
        public void BuildMutexName_ReturnsStableWindowsSafeName()
        {
            var mutexName = SingleInstancePolicy.BuildMutexName("ClipLink");

            Assert.Equal(@"Local\ClipLink", mutexName);
        }

        [Fact]
        public void BuildMutexName_ReplacesWhitespace()
        {
            var mutexName = SingleInstancePolicy.BuildMutexName("Snap Droid");

            Assert.Equal(@"Local\Snap_Droid", mutexName);
        }
    }

    public sealed class ClipLinkCliCommandParserTests
    {
        [Theory]
        [InlineData("start", ClipLinkCliCommand.Start)]
        [InlineData("stop", ClipLinkCliCommand.Stop)]
        [InlineData("status", ClipLinkCliCommand.Status)]
        [InlineData("restart", ClipLinkCliCommand.Restart)]
        public void Parse_ReturnsExpectedCommand(string commandText, ClipLinkCliCommand expected)
        {
            var command = ClipLinkCliCommandParser.Parse([commandText]);

            Assert.Equal(expected, command);
        }

        [Fact]
        public void Parse_DefaultsToStatusWhenNoArgumentsAreProvided()
        {
            var command = ClipLinkCliCommandParser.Parse([]);

            Assert.Equal(ClipLinkCliCommand.Status, command);
        }

        [Fact]
        public void Parse_ThrowsForUnsupportedCommand()
        {
            var exception = Assert.Throws<ArgumentException>(() => ClipLinkCliCommandParser.Parse(["launch"]));

            Assert.Contains("Unsupported command", exception.Message);
        }
    }

    public sealed class WorkerProcessMetadataTests
    {
        [Fact]
        public void ResolveExecutablePath_UsesWorkerExecutableName()
        {
            var path = WorkerProcessMetadata.ResolveExecutablePath(@"C:\apps\ClipLink");

            Assert.Equal(
                @"C:\apps\ClipLink\ClipLink.Worker.exe",
                path);
        }

        [Fact]
        public void ResolveExecutablePath_ThrowsForBlankBaseDirectory()
        {
            Assert.Throws<ArgumentException>(() => WorkerProcessMetadata.ResolveExecutablePath(" "));
        }
    }

    public sealed class KeyboardSequenceSenderTests
    {
        [Fact]
        public void BuildKeyTransitions_UsesExpectedOrderForTerminalPaste()
        {
            var transitions = KeyboardSequenceSender.BuildKeyTransitions("^+v");

            Assert.Equal(
                [
                    new KeyTransition(NativeMethods.VkControl, false),
                    new KeyTransition(NativeMethods.VkShift, false),
                    new KeyTransition(NativeMethods.VkV, false),
                    new KeyTransition(NativeMethods.VkV, true),
                    new KeyTransition(NativeMethods.VkShift, true),
                    new KeyTransition(NativeMethods.VkControl, true)
                ],
                transitions);
        }

        [Fact]
        public void BuildKeyTransitions_UsesExpectedOrderForShiftInsertPaste()
        {
            var transitions = KeyboardSequenceSender.BuildKeyTransitions("+{INSERT}");

            Assert.Equal(
                [
                    new KeyTransition(NativeMethods.VkShift, false),
                    new KeyTransition(NativeMethods.VkInsert, false),
                    new KeyTransition(NativeMethods.VkInsert, true),
                    new KeyTransition(NativeMethods.VkShift, true)
                ],
                transitions);
        }
    }

    public sealed class BackgroundWorkerContextTests
    {
        [Fact]
        public void ResolveHotkeyAction_UsesCopyFlowForPasteHotkey()
        {
            var action = BackgroundWorkerContext.ResolveHotkeyAction(HotkeyWindow.PasteHotkeyId);

            Assert.Equal(HotkeyAction.CopyPromptToClipboard, action);
        }

        [Fact]
        public void ResolveHotkeyAction_UsesCopyFlowForCopyHotkey()
        {
            var action = BackgroundWorkerContext.ResolveHotkeyAction(HotkeyWindow.CopyHotkeyId);

            Assert.Equal(HotkeyAction.CopyPromptToClipboard, action);
        }

        [Fact]
        public void ResolveHotkeyCandidates_PrefersConfiguredPasteHotkeyFirst()
        {
            var candidates = BackgroundWorkerContext.ResolveHotkeyCandidates(
                HotkeyWindow.PasteHotkeyId,
                "Alt+V");

            Assert.Equal(["Alt+V", "Alt+Shift+V", "Alt+F10"], candidates);
        }

        [Fact]
        public void ResolveHotkeyCandidates_AddsFallbacksForCopyHotkey()
        {
            var candidates = BackgroundWorkerContext.ResolveHotkeyCandidates(
                HotkeyWindow.CopyHotkeyId,
                "Ctrl+Shift+F10");

            Assert.Equal(["Ctrl+Shift+F10", "Ctrl+Alt+V", "Ctrl+Alt+F10"], candidates);
        }
    }
}
