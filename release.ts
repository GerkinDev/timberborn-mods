import { dirname, resolve } from "node:path";

// Function to run a command (dry-run compatible)
async function runCommand(program: string, args: string[], dryRun: boolean) {
    if (dryRun) {
        console.log(`[Dry Run] Would execute: ${program} ${args.join(" ")}`);
        return;
    }
    try {
        return await new Deno.Command(program, { args }).output();
    } catch (error) {
        console.error(`Error executing command: ${program} ${args.join(" ")}`);
        throw error;
    }
}
function isValidVersion(newVersion: string, currentVersion: string): boolean {
    const currentParts = currentVersion.split(".").map(Number);
    const newParts = newVersion.split(".").map(Number);
    for (let i = 0; i < Math.max(currentParts.length, newParts.length); i++) {
        const currentPart = currentParts[i] || 0;
        const newPart = newParts[i] || 0;
        if (newPart < currentPart) return false;
        if (newPart > currentPart) return true;
    }
    return false; // versions are equal
}

// Parse dry-run flag
const dryRunGit = Deno.args.includes("--dry-run-git") || true;

const filesToCommit: string[] = []

// List mods with parsed manifests
const mods: Array<{
    name: string;
    id: string;
    path: string;
    manifest: any;
}> = [];

const modDir = "Assets/Mods";
try {
    const entries = Deno.readDir(modDir);
    for await (const entry of entries) {
        if (entry.isDirectory) {
            const modPath = `${modDir}/${entry.name}`;
            try {
                const manifestContent = await Deno.readTextFile(resolve(modPath, "manifest.json"));
                const manifest = JSON.parse(manifestContent);
                mods.push({
                    name: manifest.Name,
                    id: manifest.Id,
                    path: modPath,
                    manifest: manifest,
                });
            } catch (e) {
                console.error(
                    `Error reading manifest for mod "${entry.name}": ${e}`,
                );
            }
        }
    }
} catch (e) {
    console.error(`Error reading directory "${modDir}": ${e}`);
}

// Select mod
if (mods.length === 0) {
    console.error("No mods found in the directory.");
    Deno.exit(1);
}

console.log("Available mods:");
for (let i = 0; i < mods.length; i++) {
    console.log(`${i + 1}. ${mods[i].name} (ID: ${mods[i].id})`);
}

const choice = prompt("Select the number of the mod you want to release: ") ??
    "";
const selectedIndex = parseInt(choice) - 1;

if (selectedIndex < 0 || selectedIndex >= mods.length) {
    console.error("Invalid selection.");
    Deno.exit(1);
}

const selectedMod = mods[selectedIndex];
console.log(`Selected mod: ${selectedMod.name} (ID: ${selectedMod.id})`);

// Get and validate new version
let newVersion: string | null;
do {
    newVersion = prompt(
        `Enter new version (currently ${selectedMod.manifest.Version}): `,
    );
} while (
    !newVersion ||
    !/^\d+\.\d+\.\d+$/.test(newVersion) &&
        !isValidVersion(newVersion, selectedMod.manifest.Version)
);

// Update manifest
selectedMod.manifest.Version = newVersion;
try {
    await Deno.writeTextFile(
        resolve(selectedMod.path, "manifest.json"),
        JSON.stringify(selectedMod.manifest, null, 2),
    );
    filesToCommit.push(resolve(selectedMod.path, "manifest.json"))
    console.log(
        `Updated manifest for mod "${selectedMod.name}" to version ${newVersion}.`,
    );
} catch (e) {
    console.error(`Failed to write manifest: ${e}`);
    Deno.exit(1);
}

// Append to changelog.md
const changelogPath = resolve(selectedMod.path, "Changelog.md");
const today = new Date();
const day = String(today.getDate()).padStart(2, "0");
const month = String(today.getMonth() + 1).padStart(2, "0");
const year = today.getFullYear();
const date = `${day}/${month}/${year}`;
const header = `[h1] v${newVersion} (${date}) [/h1]\n\n`;

// Step 8: Append to Changelog.md
const changelogContent = header + "# TODO\n";
let prevChangelogContent: string | null;
try {
    prevChangelogContent = "\n" + await Deno.readTextFile(changelogPath);
} catch (e) {
    prevChangelogContent = "";
}
const fullChangelogContent = changelogContent + prevChangelogContent;
try {
    await Deno.writeTextFile(
        changelogPath,
        fullChangelogContent,
        { create: true },
    );
    filesToCommit.push(changelogPath)
    console.log(`Appended changelog to "${changelogPath}".`);
} catch (e) {
    console.error(`Failed to update changelog: ${e}`);
    Deno.exit(1);
}
if(!confirm(`Edit the changelog at ${changelogPath} and press Enter to continue.`)){
    console.error('Abort');
    Deno.exit(1);
}
const newChangelogContent = await Deno.readTextFile(changelogPath);
const versionChangelog = newChangelogContent.match(/^\[h1\].*?\n\n(.*?)(?:\n\[h1\]|$)/s)![1].trim();
console.log('Version changelog:')
console.log('------')
console.log(versionChangelog)
console.log('------')

// Git operations (dry-run compatible)
await runCommand("git", ["add", ...filesToCommit], dryRunGit);
await runCommand(
    "git",
    ["commit", "-m", `chore(${selectedMod.id}): release v${newVersion}\n\n${versionChangelog}`],
    dryRunGit,
);
await runCommand("git", ["tag", newVersion, "-m", versionChangelog], dryRunGit);
await runCommand("git", ["push", "origin", "main", "--tags"], dryRunGit);
