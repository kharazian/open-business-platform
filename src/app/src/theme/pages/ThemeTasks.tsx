import { CircleDot, Kanban, ListChecks, Plus, Search, Timer } from "lucide-react";
import { themeTasks } from "../mockData";
import { Badge } from "../../components/ui/Badge";
import { Button } from "../../components/ui/Button";
import { Card } from "../../components/ui/Card";
import { Input } from "../../components/ui/Input";
import { PageHeader } from "../../components/ui/PageHeader";
import { Progress } from "../../components/ui/Progress";
import { Select } from "../../components/ui/Select";
import { StatCard } from "../../components/ui/StatCard";
import { Table } from "../../components/ui/Table";
import { ThemeHeaderAction } from "../components/ThemeHeaderAction";

const taskColumns = ["Todo", "In Progress", "Done"] as const;

function getPriorityTone(priority: string) {
  if (priority === "Critical") return "danger";
  if (priority === "High") return "warning";
  return "info";
}

export function ThemeTasks() {
  const activeTasks = themeTasks.filter((task) => task.status !== "Done").length;

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Execution"
        title="Tasks"
        description="Track operational work with board columns, ownership, priority badges, and a table view."
        actions={<ThemeHeaderAction icon={Plus}>New task</ThemeHeaderAction>}
      />

      <div className="grid gap-4 md:grid-cols-3">
        <StatCard label="Active tasks" value={String(activeTasks)} hint="Open or in progress" icon={ListChecks} />
        <StatCard label="Critical" value="1" hint="Requires review" icon={CircleDot} tone="warning" />
        <StatCard label="Completion" value="62%" hint="Sample board average" icon={Timer} tone="success" />
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        {taskColumns.map((column) => {
          const columnTasks = themeTasks.filter((task) => task.status === column);

          return (
            <section className="rounded-xl border border-border bg-muted/20 p-4" key={column}>
              <div className="mb-4 flex items-center justify-between gap-3">
                <div>
                  <h2 className="text-sm font-black uppercase text-foreground">{column}</h2>
                  <p className="mt-1 text-sm text-muted-foreground">{columnTasks.length} sample tasks</p>
                </div>
                <Badge tone={column === "Done" ? "success" : "info"}>{columnTasks.length}</Badge>
              </div>

              <div className="space-y-3">
                {columnTasks.map((task) => (
                  <Card className="p-4" key={task.title}>
                    <div className="flex items-start justify-between gap-3">
                      <div className="min-w-0">
                        <p className="font-bold text-foreground">{task.title}</p>
                        <p className="mt-1 text-sm text-muted-foreground">
                          {task.area} / {task.owner}
                        </p>
                      </div>
                      <Badge tone={getPriorityTone(task.priority)}>{task.priority}</Badge>
                    </div>
                    <div className="mt-4">
                      <Progress value={task.progress} label={task.due} />
                    </div>
                  </Card>
                ))}
              </div>
            </section>
          );
        })}
      </div>

      <Card title="Task list" description="Table view for the same task sample data.">
        <div className="grid gap-3 pb-5 md:grid-cols-[1fr_170px_170px_auto]">
          <Input icon={<Search size={16} />} placeholder="Search tasks..." />
          <Select>
            <option>All areas</option>
            <option>Users</option>
            <option>Roles</option>
            <option>Reports</option>
          </Select>
          <Select>
            <option>All priorities</option>
            <option>Critical</option>
            <option>High</option>
            <option>Normal</option>
          </Select>
          <Button variant="outline">Filter</Button>
        </div>

        <Table
          columns={[
            { header: "Task", accessor: "title" },
            { header: "Owner", accessor: "owner" },
            { header: "Area", accessor: "area" },
            { header: "Due", accessor: "due" },
            { header: "Status", accessor: "status" },
            { header: "Priority", render: (task) => <Badge tone={getPriorityTone(task.priority)}>{task.priority}</Badge> }
          ]}
          data={themeTasks}
        />
      </Card>
    </div>
  );
}
