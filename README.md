# Temperature Alert Escalation Temporal Workflow Demo

This repository contains two .NET projects showcasing how to work with [Temporal](https://temporal.io/) for building and executing workflows:

* **TempAlertClient**: A client application that starts a workflow.
* **TempAlertWorker**: A worker application that hosts the workflow and its activities.

## 📦 Project Structure

```
/TempAlertClient   → Triggers workflows via Temporal Client
/TempAlertWorker   → Hosts workflow and activity implementations
```

---

## 🚀 Prerequisites

* [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download)
* [Temporal CLI](https://docs.temporal.io/cli/tctl) installed
* Docker (for local Temporal server)
* Windows OS (instructions below are tailored for Windows)

---

## 🛠️ Setting Up Temporal Locally

Follow these steps to install and run a local Temporal development server using the Temporal CLI:

1. **Install Temporal CLI**
   Download the Temporal CLI executable for Windows:
   [Download temporal.exe](https://github.com/temporalio/cli/releases)

   Add it to your `PATH`.

2. **Start the Temporal Server with Docker**
   Run the following command in your terminal:

   ```bash
   temporal server start-dev
   ```

   This spins up Temporal Server with a Postgres database and Web UI at `http://localhost:8233`.

---

## ▶️ Running the Projects

1. **Start the Worker**

   Open a terminal in the `TempAlertWorker` project directory and run:

   ```bash
   dotnet run
   ```

   This registers the worker with the Temporal service and starts listening for workflows.

2. **Start the Client**

   In a separate terminal, navigate to the `TempAlertClient` directory and run:

   ```bash
   dotnet run
   ```

   This sends a command to start the workflow on Temporal.

---

## 👀 Viewing Workflows and Activities

Once your client has started a workflow and the worker is processing it:

* Go to [http://localhost:8233](http://localhost:8233) to open the Temporal Web UI.
* You can view:

  * Workflow executions
  * Activity progress
  * Event history
  * Logs and state transitions

---

## 📬 Triggering New Workflows

Every time you run the `TempAlertClient`, it will start a new workflow execution. You can observe each run independently in the Temporal UI.

---

## 💡 Notes

* This project is for learning/demo purposes.
* You can expand the workflow with timers, retries, child workflows, etc.
* To run with a production Temporal cluster or Temporal Cloud, adjust the connection settings in your client and worker.

---

Let me know if you want this as a `README.md` file or tailored for deployment to a team/org.
