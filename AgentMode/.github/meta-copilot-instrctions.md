# Meta Copilot Instructions: VS Code Customization Management

## ⚠️ CRITICAL: Meta-Meta-Instruction Compliance Requirements ⚠️

**FOR ALL CODE REVIEWS OF THIS FILE:**
- **MUST** follow guidance in `.github/meta-meta-copilot-instructions.md` for all changes to this file
- **MUST** apply VS Code Copilot customization best practices (instruction file organization, prompt file management, workspace integration)
- **MUST** maintain VS Code terminology scope only (no repository-specific content)
- **MUST** validate VS Code instruction file architecture integrity when making changes

## Nomenclature Scope
**REQUIRED**: This document MUST ONLY use terminology from VS Code Copilot customization features as documented at https://code.visualstudio.com/docs/copilot/copilot-customization

**PERMITTED TERMS**:
- `.github/copilot-instructions.md` files
- `.instructions.md` files (with front matter: description, applyTo)
- `.prompt.md` files (with front matter: mode, model, tools, description)
- Custom instructions, prompt files, VS Code settings
- Front matter, Markdown formatting, glob patterns
- Chat modes (ask, edit, agent), workspace/user scopes

**PROHIBITED**: This document must NOT contain any repository-specific technical details, domain knowledge, or content about what the actual codebase does.

## Overview
This document provides guidance for managing VS Code Copilot customization files. It defines when and how to update the main `.github/copilot-instructions.md` file and when to create specialized instruction and prompt files using VS Code's Copilot customization features.

## ⚠️ CRITICAL: Specialized Instructions Enforcement ⚠️

**PREVENTION RULE**: This meta-instruction system exists to prevent repeated failures where specialized instructions are ignored.

### Common Failure Pattern (MUST PREVENT)
1. **Problem**: User requests specialized operation (e.g., PR description update)
2. **Wrong Approach**: Agent attempts operation without reading specialized instructions
3. **Failure**: Operation fails due to not following established procedures
4. **User Frustration**: User points to `.github` folder instructions that already exist
5. **Delayed Success**: Agent finally reads instructions and succeeds

### Enforcement Protocol
**MANDATORY SEQUENCE** for ANY specialized operation:

1. **IMMEDIATE CHECK**: Before attempting ANY operation that might have specialized instructions, check for relevant `.instructions.md` files
2. **READ FIRST**: If specialized instructions exist, read them COMPLETELY before proceeding
3. **FOLLOW EXACTLY**: Use the exact methods specified in the instructions
4. **NO IMPROVISATION**: Do not attempt alternative approaches until following the documented method
5. **VERIFY SUCCESS**: Always verify using the methods specified in the instructions

### High-Risk Operations That MUST Check Instructions First
- **PR description updates**: ALWAYS check `azure-cli.instructions.md` first
- **Pull request creation**: ALWAYS check `pr-management.instructions.md` first  
- **Build issues**: ALWAYS check `build-troubleshooting.instructions.md` first
- **File operations**: ALWAYS check `file-operations.instructions.md` first

**🚨 ABSOLUTE RULE**: If a user mentions looking in `.github` folder or mentions "instructions", immediately stop current approach and read the relevant specialized instruction file before continuing.

**🚨 ABSOLUTE RULE**: If a user mentions looking in `.github` folder or mentions "instructions", immediately stop current approach and read the relevant specialized instruction file before continuing.

## VS Code Copilot Customization File Architecture

**DEFINITION**: The VS Code Copilot customization system is built around four core file types that represent the complete set of artifacts that GitHub Copilot can create and manage through the chat process.

### Core File Types in VS Code Copilot Customization System

#### 1. Main Instructions File
- **Artifact**: Single `copilot-instructions.md` file at `.github/copilot-instructions.md`
- **Purpose**: Universal project-wide development patterns and coding standards
- **Scope**: Applies to all development work in the workspace
- **VS Code Integration**: Automatically included in every chat request when `github.copilot.chat.codeGeneration.useInstructionFiles` is enabled

#### 2. Specialized Instructions Files
- **Artifacts**: Multiple `*.instructions.md` files in `.github/instructions/` folder
- **Purpose**: Context-specific guidance that auto-applies based on file patterns
- **Scope**: Targeted to specific file types or development contexts via `applyTo` front matter
- **VS Code Integration**: Automatically activated when working with matching file patterns

#### 3. Prompt Files
- **Artifacts**: Multiple `*.prompt.md` files in `.github/prompts/` folder
- **Purpose**: Standardized, reusable prompts for frequent development tasks
- **Scope**: Task-oriented procedures that can be executed on-demand
- **VS Code Integration**: Executed via `/promptname` syntax or Chat: Run Prompt command

#### 4. Workspace Tasks
- **Primary Artifact**: Task definitions in `.vscode/tasks.json`
- **Supporting Artifacts**: PowerShell/shell scripts in `.scripts/` folder
- **Purpose**: Parameterized, executable procedures for complex operations
- **Scope**: Workspace-level automation that integrates with VS Code's task system
- **VS Code Integration**: Executed via Tasks: Run Task command or programmatically via tools

### File Organization Principles
- **Single Source Control**: The `copilot-instructions.md` file serves as the primary controller for creating and managing all other file types
- **Hierarchical Organization**: Main instructions → Specialized instructions → Prompt files → Workspace tasks
- **Separation of Concerns**: Each file type handles different aspects of development guidance and automation
- **VS Code Native Integration**: All file types leverage VS Code's built-in features rather than external tooling

**CRITICAL**: These four file types represent the complete universe of artifacts that GitHub Copilot should create through chat interactions for VS Code workspace customization. No other file types or locations should be created for Copilot customization purposes.

### File Architecture Integrity Enforcement

**MANDATORY RULE**: The `copilot-instructions.md` file MUST NEVER violate the four file type system by creating additional folders or file types beyond those defined above.

**Specific Prohibitions**:
- **DO NOT** create `.github/scripts/` folder - use existing `.scripts/` folder for PowerShell/shell scripts
- **DO NOT** create new folder structures outside the four defined file types
- **DO NOT** create file types not explicitly supported by VS Code Copilot customization features
- **DO NOT** bypass the established file architecture for any development automation needs

**🚨 CRITICAL VIOLATION: .github/scripts/ folder was previously created in violation of file architecture integrity. This folder has been removed and MUST NEVER be recreated.**

**Enforcement Strategy**: All changes to `copilot-instructions.md` must be validated against the file architecture integrity before implementation. Any instructions that would create files outside the four file types must be redesigned to use existing file types properly.

### Violation Detection and Prevention

**MANDATORY PRE-CHECK**: Before creating any files or folders, verify compliance with the four file type system:

1. **Main Instructions**: Only `.github/copilot-instructions.md`
2. **Specialized Instructions**: Only `.github/instructions/*.instructions.md`
3. **Prompt Files**: Only `.github/prompts/*.prompt.md`
4. **Workspace Tasks**: Only `.vscode/tasks.json` + `.scripts/*.ps1`

**AUTOMATIC VIOLATIONS TO PREVENT**:
- ❌ Creating `.github/scripts/` (use `.scripts/` instead)
- ❌ Creating `.github/docs/` (use `docs/` at repository root instead)
- ❌ Creating new root-level folders for automation
- ❌ Creating non-VS Code customization file types in `.github/`

**IF UNSURE**: Always check existing file type locations first. All PowerShell scripts belong in `.scripts/`, not `.github/scripts/`.

## File Type Guidelines

### .github/copilot-instructions.md (Main Instructions)
**Purpose**: Project-wide development patterns and coding standards that apply to all development work in the workspace.

**VS Code Behavior**: Automatically included in every chat request when `github.copilot.chat.codeGeneration.useInstructionFiles` setting is `true`.

**Content Should Include**:
- Universal coding patterns and architectural principles
- Essential development standards that apply across all files
- Core procedural knowledge that all developers need
- Universal quality and workflow standards

**Content Should NOT Include**:
- Task-specific procedures that only apply to certain scenarios
- Detailed command sequences for specific operations
- Temporary workarounds or experimental approaches
- Content that could be better served by specialized `.instructions.md` files

### .instructions.md Files (Specialized Instructions)
**Purpose**: Task-specific guidance that applies automatically based on file patterns using VS Code's `applyTo` front matter property.

**VS Code Behavior**: 
- Stored in `.github/instructions/` folder by default
- Automatically included when `applyTo` glob pattern matches current file
- Can be manually attached via Chat view > Add Context > Instructions

**When to Create**:
- When guidance applies only to specific file types or directories
- When instructions are complex enough to warrant separation but used repeatedly
- When different contexts need different instruction sets for the same workspace
- When experimental practices need testing before inclusion in main instructions

**File Structure Requirements**:
```markdown
---
description: "Brief description of what this instruction file covers"
applyTo: "**/*.{pattern}" # or specific glob pattern
---

# Instruction content in Markdown format
```

**VS Code Integration**:
- Files automatically discovered in `.github/instructions/` folder
- Use `chat.instructionsFilesLocations` setting to add more instruction folders
- Reference other files using Markdown links with relative paths

### .prompt.md Files (Reusable Prompts)
**Purpose**: Standardized prompts for common tasks using VS Code's experimental prompt file feature.

**VS Code Behavior**:
- Stored in `.github/prompts/` folder by default
- Executed via `/promptname` in chat or Chat: Run Prompt command
- Can include variables like `${selection}`, `${input:name}`, `${workspaceFolder}`

**When to Create**:
- For frequently repeated code generation tasks
- For standardized analysis or review procedures
- For specific setup or configuration tasks
- For complex queries that benefit from predefined structure

**File Structure Requirements**:
```markdown
---
description: "What this prompt does"
mode: "ask" # or "edit" or "agent"
model: "gpt-4o" # optional, specific model to use
tools: ["tool1", "tool2"] # if agent mode, tools available
---

# Prompt content with variables like ${selection} or ${input:name:placeholder}
```

**VS Code Integration**:
- Use `chat.promptFilesLocations` setting to add more prompt folders
- Reference other prompt/instruction files using Markdown links
- Variables automatically replaced when prompt is executed

## Update Procedures

### Before Making Changes

#### Assessment Phase
1. **Identify Update Type**:
   - **Content Addition**: New patterns, procedures, or guidance
   - **Content Modification**: Updates to existing guidance
   - **Content Removal**: Deprecating outdated information
   - **Restructuring**: Moving content between file types

2. **Determine Appropriate VS Code File Type**:
   - Universal guidance → Main `copilot-instructions.md`
   - Context-specific guidance → Create/update `.instructions.md` with `applyTo` pattern
   - Reusable task procedures → Create/update `.prompt.md` with appropriate mode

3. **VS Code Integration Analysis**:
   - Will this change affect how instructions are automatically applied?
   - Does this conflict with or duplicate existing instruction files?
   - Should this replace existing content rather than supplement it?
   - How will glob patterns in `applyTo` be affected?

#### Validation Requirements
1. **VS Code Feature Validation**:
   - Test that `.instructions.md` files activate properly based on `applyTo` patterns
   - Verify `.prompt.md` files execute correctly with variables and tools
   - Ensure main instructions appear automatically in chat requests
   - Test that front matter metadata is properly formatted

2. **Content Validation**:
   - Check for consistency with VS Code documentation requirements
   - Verify Markdown formatting is valid and renders properly
   - Ensure file paths and references work correctly in VS Code context

### Making Changes

#### For Main Copilot Instructions (.github/copilot-instructions.md)
1. **VS Code Settings**: Ensure `github.copilot.chat.codeGeneration.useInstructionFiles` is `true`
2. **File Location**: Must be at `.github/copilot-instructions.md` in workspace root
3. **Content Organization**: Maintain logical section structure using Markdown headers
4. **Cross-References**: Use Markdown links to reference other instruction/prompt files
5. **Consistency**: Maintain consistent Markdown formatting and terminology

#### For Specialized Instruction Files (.instructions.md)
1. **File Location**: 
   - Default: `.github/instructions/` folder
   - Additional locations via `chat.instructionsFilesLocations` setting
2. **Front Matter Requirements**: Include valid `description` and `applyTo` properties
3. **Glob Patterns**: Test `applyTo` patterns match intended file types
4. **Content Focus**: Keep content focused on specific context without duplicating main instructions
5. **References**: Use relative paths for Markdown links to other files

#### For Prompt Files (.prompt.md)
1. **File Location**:
   - Default: `.github/prompts/` folder  
   - Additional locations via `chat.promptFilesLocations` setting
2. **Front Matter Requirements**: Include `description`, `mode`, and optional `model`/`tools`
3. **Variable Usage**: Use VS Code variables like `${selection}`, `${input:name:placeholder}`, `${workspaceFolder}`
4. **Mode Selection**: Choose appropriate mode (ask/edit/agent) for intended usage
5. **Tool Configuration**: For agent mode, specify required tools in front matter

### After Making Changes

#### VS Code Copilot Testing Protocol
1. **Instruction File Activation**:
   - Test that `.instructions.md` files automatically apply based on `applyTo` patterns
   - Verify main instructions appear in chat requests when setting is enabled
   - Check that manual instruction attachment works via Chat view

2. **Prompt File Execution**:
   - Test prompt files execute correctly using `/promptname` syntax
   - Verify variables are properly substituted (selection, input, workspace)
   - Ensure correct chat mode (ask/edit/agent) behavior
   - Test tool availability in agent mode prompts

3. **VS Code Integration**:
   - Check that Configure Chat button shows instruction/prompt files properly
   - Verify file discovery in default and custom locations
   - Test Quick Pick functionality for file selection
   - Ensure Settings Sync works for user-scoped files (if configured)

#### Content Quality Validation
1. **Markdown Rendering**: Ensure all Markdown formatting renders correctly in VS Code chat
2. **Link Functionality**: Test that Markdown links between files work properly
3. **Variable Substitution**: Verify all VS Code variables expand correctly in prompts
4. **Front Matter Validation**: Check that YAML front matter is properly formatted and recognized

#### Documentation
1. **Change Documentation**: Update relevant sections to reflect changes
2. **Version Notes**: Consider documenting significant changes for team awareness
3. **Cross-Reference Updates**: Ensure all internal links and references remain valid

## VS Code Copilot Feature Usage Strategy

### File Organization Best Practices

#### Main Instructions File
- **Purpose**: Universal coding standards and patterns that apply workspace-wide
- **Automatic Inclusion**: Always included in chat requests when setting is enabled
- **Content**: High-level principles, coding standards, universal procedures
- **Size Guidelines**: Keep manageable for VS Code performance (recommend under 500 lines)

#### Specialized Instruction Files
- **Purpose**: Context-specific guidance that auto-applies based on file patterns
- **Pattern Matching**: Use glob patterns in `applyTo` front matter for automatic activation
- **Content Focus**: Detailed guidance for specific file types or development contexts
- **Examples**: 
  - `typescript.instructions.md` with `applyTo: "**/*.ts,**/*.tsx"`
  - `testing.instructions.md` with `applyTo: "**/*.test.*,**/*.spec.*"`
  - `documentation.instructions.md` with `applyTo: "**/*.md"`

#### Prompt Files for Common Tasks
- **Purpose**: Standardized, reusable prompts for frequent development tasks
- **Execution**: Via `/promptname` or Chat: Run Prompt command
- **Variable Usage**: Leverage VS Code variables for dynamic content
- **Examples**:
  - `code-review.prompt.md` for standardized code review procedures
  - `write-tests.prompt.md` for test generation patterns
  - `document-api.prompt.md` for API documentation creation

#### Task Complexity Management Strategy
**PRINCIPLE**: Keep `.github` folder content focused on guidance rather than complex procedures. When possible, prefer creating VS Code tasks over detailed step-by-step procedures in Markdown files.

**Task Creation Guidelines**:
- **Complex Procedures**: Instead of embedding long command sequences in `.prompt.md` files, create VS Code tasks with parameters
- **Script Location**: Place PowerShell/shell scripts in `.scripts` folder (or repository-appropriate script folder)
- **Task Definition**: Define parameterized tasks in `.vscode/tasks.json` with input prompts for user values
- **Markdown References**: Keep `.prompt.md` files focused on describing what to do, with references to the appropriate VS Code task

**Benefits of Task-Based Approach**:
- **Maintainability**: Scripts can be updated independently of instruction content
- **Reusability**: Tasks can be called programmatically or manually executed
- **User Experience**: VS Code provides better UX for parameter input and execution feedback
- **Debugging**: Task output appears in VS Code's integrated terminal with proper error handling

**Example Pattern**:
```markdown
<!-- Instead of this in a .prompt.md file: -->
Run these commands:
1. Set environment variables...
2. Execute complex PowerShell script with parameters...
3. Validate results...

<!-- Prefer this approach: -->
Use the "Update PR Description" VS Code task (Ctrl+Shift+P → Tasks: Run Task)
This task is defined in .vscode/tasks.json and executes .scripts/update-pr-description.ps1
```

### VS Code Settings Integration

#### Required Settings
```json
{
  "github.copilot.chat.codeGeneration.useInstructionFiles": true,
  "chat.promptFiles": true
}
```

#### Optional Customization Settings
```json
{
  "chat.instructionsFilesLocations": [".github/instructions", "docs/copilot"],
  "chat.promptFilesLocations": [".github/prompts", "scripts/prompts"]
}
```

### Content Migration Strategy

#### From Monolithic to Modular
1. **Identify Universal Content**: Keep in main `.github/copilot-instructions.md`
2. **Extract Context-Specific Content**: Move to `.instructions.md` files with appropriate `applyTo` patterns
3. **Convert Procedures to Prompts**: Transform step-by-step procedures into reusable `.prompt.md` files
4. **Test Pattern Matching**: Verify `applyTo` glob patterns activate correctly

#### File Structure Recommendations
```
.github/
├── copilot-instructions.md           # Universal standards
├── instructions/
│   ├── backend.instructions.md       # applyTo: "**/src/backend/**"
│   ├── frontend.instructions.md      # applyTo: "**/src/frontend/**"
│   └── configuration.instructions.md # applyTo: "**/*.config.*,**/*.json"
└── prompts/
    ├── create-component.prompt.md     # mode: edit
    ├── review-security.prompt.md      # mode: ask
    └── generate-docs.prompt.md        # mode: agent
```

## Quality Standards

### VS Code Integration Quality
- **Front Matter Validation**: All `.instructions.md` and `.prompt.md` files must have valid YAML front matter
- **Pattern Testing**: `applyTo` glob patterns must be tested to ensure they match intended file types
- **Variable Functionality**: All VS Code variables in prompt files must expand correctly
- **Settings Compatibility**: Files must work with both default and custom location settings

### Content Quality
- **Markdown Compliance**: All content must use valid Markdown formatting that renders properly in VS Code chat
- **Link Functionality**: Relative links between instruction/prompt files must work correctly
- **Clarity**: Instructions should be clear and actionable for AI assistance
- **Specificity**: Avoid vague guidance that doesn't improve AI responses

### File Organization Quality
- **Single Responsibility**: Each file should have one clear purpose
- **Appropriate Scope**: Content should match the file type (universal vs. context-specific vs. task-oriented)
- **Maintainability**: Files should be easy to update without affecting other files
- **Discoverability**: Files should be named and organized for easy finding in VS Code UI

## Integration with VS Code Features

### Settings Synchronization
- **User Files**: Use Settings Sync to share user instruction/prompt files across devices
- **Workspace Files**: Version control workspace files for team sharing
- **Configuration**: Document required settings for proper functionality

### Chat Integration
- **Automatic Inclusion**: Test that main instructions and matching specialized instructions appear automatically
- **Manual Attachment**: Verify that manual attachment via Chat view works correctly
- **Prompt Execution**: Ensure `/promptname` syntax works and variables substitute properly

### Performance Considerations
- **File Size**: Keep individual files reasonably sized for VS Code performance
- **Pattern Complexity**: Use efficient glob patterns that don't slow down file matching
- **Reference Chains**: Avoid overly complex chains of file references that could cause circular dependencies

## Historical Violations (NEVER REPEAT)

**🚨 DOCUMENTED FAILURES**: These exact violations occurred and were corrected. They MUST NEVER be repeated:

### Violation 1: .github/scripts/ Folder (August 2025)
- **Created**: `.github/scripts/replace-connection-strings.ps1`
- **Violation**: PowerShell scripts belong in `.scripts/` folder, not `.github/scripts/`
- **Impact**: Violated four file type VS Code customization system
- **Corrected**: Removed folder, enhanced enforcement in instructions
- **Prevention**: Enhanced meta-copilot-instructions.md with specific prohibitions

### Violation 2: .github/docs/ Folder (August 2025)  
- **Created**: `.github/docs/generic-exception-prevention.md`
- **Violation**: Documentation belongs in `docs/` at repository root, not `.github/docs/`
- **Impact**: Violated four file type VS Code customization system
- **Corrected**: Moved content to `docs/generic-exception-prevention.md`, removed folder
- **Prevention**: Updated instructions to specify `docs/` as proper documentation location

**ENFORCEMENT PROTOCOL**: If these specific folders are ever recreated, immediately:
1. STOP all current operations
2. DELETE the violating folder completely  
3. MOVE content to proper location (`docs/` for documentation, `.scripts/` for PowerShell)
4. ENHANCE prevention mechanisms further
5. DOCUMENT the repeated violation for additional enforcement

This guidance ensures that VS Code Copilot customization features are used effectively while maintaining system quality and performance.
