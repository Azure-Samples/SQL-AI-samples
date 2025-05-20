
# VSCode Agent Mode SQL Database Project Sample

This workspace is a sample for using VSCode Agent Mode to scaffold and test an application using SQL Database projects.

## Getting Started

1. **Open the `business-process-description.md` file**
   - Path: `ConfMSmini/business-process-description.md`
   - Type `Follow instructions in current file` into the Agent-Mode chat window.

2. **Agent Mode Workflow**
   - The process is interactive. You will be prompted to type `proceed` multiple times as you move through the steps.
   - The agent will scaffold the project, generate SQL scripts, and set up tests according to the business process description.
   - The agent should proceed until all steps are complete and all tests (that represent the User Stories) pass.

3. **Requirements**
   - This sample was tested with the GPT-4.1 model.
   - **A container runtime (e.g., Docker or Podman) must be installed and running before you begin.**
   - All other dependencies (such as sqlcmd, SqlPackage, etc.) will be checked for and installed by the Agent as needed.

4. **Project Structure**
   - The project will be organized into folders for tables, views, programmability, pre/post deployment scripts, and tests, following best practices for SQL Database Projects.
