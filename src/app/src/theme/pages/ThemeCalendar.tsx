import { CalendarDays, Clock, Plus, Search, Users } from "lucide-react";
import { themeCalendarEvents } from "../mockData";
import { Badge } from "../../components/ui/Badge";
import { Button } from "../../components/ui/Button";
import { Card } from "../../components/ui/Card";
import { Input } from "../../components/ui/Input";
import { PageHeader } from "../../components/ui/PageHeader";
import { Select } from "../../components/ui/Select";
import { StatCard } from "../../components/ui/StatCard";
import { Table } from "../../components/ui/Table";
import { cn } from "../../lib/cn";
import { useThemeAppearance } from "../../context/ThemeAppearanceContext";

const weekDays = [
  { day: "Mon", date: "11", events: 1 },
  { day: "Tue", date: "12", events: 2 },
  { day: "Wed", date: "13", events: 1 },
  { day: "Thu", date: "14", events: 0 },
  { day: "Fri", date: "15", events: 2 },
  { day: "Sat", date: "16", events: 0 },
  { day: "Sun", date: "17", events: 1 }
];

function getEventTone(status: string) {
  if (status === "Confirmed") return "success";
  if (status === "Scheduled") return "info";
  return "warning";
}

export function ThemeCalendar() {
  const { palette } = useThemeAppearance();

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Planning"
        title="Calendar"
        description="Coordinate workspace events, reviews, operational meetings, and scheduled tasks."
        actions={
          <Button>
            <Plus size={16} />
            Add event
          </Button>
        }
      />

      <div className="grid gap-4 md:grid-cols-3">
        <StatCard label="Events this week" value="7" hint="Across 4 teams" icon={CalendarDays} />
        <StatCard label="Next meeting" value="09:00" hint="Access review" icon={Clock} tone="warning" />
        <StatCard label="Attendees" value="31" hint="Sample workspace total" icon={Users} tone="success" />
      </div>

      <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_360px]">
        <Card title="Weekly schedule" description="Calendar grid preview for busy days and scheduled workspace events.">
          <div className="grid grid-cols-7 gap-2">
            {weekDays.map((day) => (
              <div className="min-h-32 rounded-xl border border-border bg-muted/20 p-3" key={day.date}>
                <div className="flex items-center justify-between gap-2">
                  <div>
                    <p className="text-xs font-bold uppercase text-muted-foreground">{day.day}</p>
                    <p className="mt-1 text-xl font-black text-foreground">{day.date}</p>
                  </div>
                  {day.events ? <Badge tone="info">{day.events}</Badge> : null}
                </div>
                <div className="mt-4 space-y-2">
                  {themeCalendarEvents
                    .filter((event) => event.date.endsWith(day.date))
                    .map((event) => (
                      <div className={cn("rounded-lg px-3 py-2 text-xs font-bold", palette.softBg, palette.softText)} key={event.title}>
                        {event.time} {event.title}
                      </div>
                    ))}
                </div>
              </div>
            ))}
          </div>
        </Card>

        <Card title="Today" description="Compact agenda card for the selected day.">
          <div className="space-y-4">
            {themeCalendarEvents.slice(0, 3).map((event) => (
              <div className="flex gap-3" key={event.title}>
                <div className={cn("grid size-11 shrink-0 place-items-center rounded-xl", palette.softBg, palette.softText)}>
                  <Clock size={18} />
                </div>
                <div className="min-w-0">
                  <p className="font-bold text-foreground">{event.title}</p>
                  <p className="mt-1 text-sm text-muted-foreground">
                    {event.date} at {event.time} / {event.owner}
                  </p>
                </div>
              </div>
            ))}
          </div>
        </Card>
      </div>

      <Card title="Event list">
        <div className="grid gap-3 pb-5 md:grid-cols-[1fr_170px_170px_auto]">
          <Input icon={<Search size={16} />} placeholder="Search events..." />
          <Select>
            <option>All owners</option>
            <option>Security</option>
            <option>Operations</option>
            <option>Compliance</option>
          </Select>
          <Select>
            <option>All statuses</option>
            <option>Confirmed</option>
            <option>Scheduled</option>
            <option>Draft</option>
          </Select>
          <Button variant="outline">Filter</Button>
        </div>

        <Table
          columns={[
            { header: "Event", accessor: "title" },
            { header: "Owner", accessor: "owner" },
            { header: "Date", accessor: "date" },
            { header: "Time", accessor: "time" },
            { header: "Attendees", accessor: "attendees" },
            { header: "Status", render: (event) => <Badge tone={getEventTone(event.status)}>{event.status}</Badge> }
          ]}
          data={themeCalendarEvents}
        />
      </Card>
    </div>
  );
}
