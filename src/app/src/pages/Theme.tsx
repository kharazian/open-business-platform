import { ExternalLink, Palette, Route, SwatchBook } from "lucide-react";
import { Link } from "react-router-dom";
import { Badge } from "../components/ui/Badge";
import { Button } from "../components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../components/ui/Card";
import { Input } from "../components/ui/Input";
import { Modal } from "../components/ui/Modal";
import { Select } from "../components/ui/Select";
import { Table, type TableColumn } from "../components/ui/Table";
import { navigationItems } from "../lib/data";
import { useState } from "react";

const colorTokens = [
  { name: "Primary", className: "bg-primary", text: "text-primary" },
  { name: "Primary soft", className: "bg-primary-soft", text: "text-primary" },
  { name: "Indigo", className: "bg-indigo", text: "text-indigo" },
  { name: "Amber", className: "bg-amber", text: "text-amber" },
  { name: "Rose", className: "bg-rose", text: "text-rose" },
  { name: "Success", className: "bg-success", text: "text-success" },
  { name: "Warning", className: "bg-warning", text: "text-warning" },
  { name: "Danger", className: "bg-danger", text: "text-danger" },
  { name: "Card", className: "bg-card", text: "text-foreground" },
  { name: "Muted", className: "bg-muted", text: "text-muted-foreground" }
];

const routeRows = navigationItems.map((item) => ({
  label: item.label,
  path: item.path,
  location: "src/app/src/lib/data.ts"
}));

type RouteRow = (typeof routeRows)[number];

const routeColumns: Array<TableColumn<RouteRow>> = [
  {
    header: "Link",
    accessor: "label",
    render: (row) => (
      <Link className="font-bold text-primary hover:underline" to={row.path}>
        {row.label}
      </Link>
    )
  },
  { header: "Path", accessor: "path" },
  { header: "Defined in", accessor: "location" }
];

export function Theme() {
  const [modalOpen, setModalOpen] = useState(false);

  return (
    <div className="grid gap-6">
      <section className="surface p-6">
        <Badge variant="info">
          <Palette className="mr-1 size-3.5" />
          Theme system
        </Badge>
        <h1 className="mt-4 text-3xl font-bold text-foreground sm:text-4xl">Theme and navigation guide</h1>
        <p className="mt-3 max-w-3xl text-base leading-7 text-muted-foreground">
          Use this page to preview colors, components, route links, and the light/dark theme. The best place for theme
          tokens is <strong className="text-foreground">src/app/src/styles.css</strong>. The best place for sidebar links
          is <strong className="text-foreground">src/app/src/lib/data.ts</strong>.
        </p>
      </section>

      <section className="grid gap-6 xl:grid-cols-[1fr_24rem]">
        <Card>
          <CardHeader>
            <CardTitle>
              <SwatchBook className="mr-2 inline size-5 text-primary" />
              Color tokens
            </CardTitle>
            <CardDescription>These Tailwind names come from the app theme tokens.</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
              {colorTokens.map((token) => (
                <div className="rounded-xl border border-border bg-muted/45 p-3" key={token.name}>
                  <div className={`h-16 rounded-lg border border-border ${token.className}`} />
                  <div className="mt-3 flex items-center justify-between gap-3">
                    <p className="font-bold text-foreground">{token.name}</p>
                    <code className={`rounded-md bg-card px-2 py-1 text-xs font-bold ${token.text}`}>{token.className}</code>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Where to add things</CardTitle>
            <CardDescription>Keep theme and navigation simple while the project grows.</CardDescription>
          </CardHeader>
          <CardContent className="grid gap-3">
            <div className="rounded-xl border border-border bg-muted/45 p-4">
              <p className="font-bold text-foreground">Theme tokens</p>
              <p className="mt-1 text-sm leading-6 text-muted-foreground">Add colors, radii, shadows, and base styles in:</p>
              <code className="mt-3 block rounded-lg bg-card px-3 py-2 text-sm text-primary">src/app/src/styles.css</code>
            </div>
            <div className="rounded-xl border border-border bg-muted/45 p-4">
              <p className="font-bold text-foreground">Sidebar links</p>
              <p className="mt-1 text-sm leading-6 text-muted-foreground">Add new menu items to:</p>
              <code className="mt-3 block rounded-lg bg-card px-3 py-2 text-sm text-primary">src/app/src/lib/data.ts</code>
            </div>
            <div className="rounded-xl border border-border bg-muted/45 p-4">
              <p className="font-bold text-foreground">Routes</p>
              <p className="mt-1 text-sm leading-6 text-muted-foreground">Add matching route entries in:</p>
              <code className="mt-3 block rounded-lg bg-card px-3 py-2 text-sm text-primary">src/app/src/App.tsx</code>
            </div>
          </CardContent>
        </Card>
      </section>

      <Card>
        <CardHeader>
          <CardTitle>
            <Route className="mr-2 inline size-5 text-primary" />
            App links
          </CardTitle>
          <CardDescription>All current sidebar links are centralized and rendered from one array.</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="overflow-x-auto">
            <div className="min-w-[44rem]">
              <Table columns={routeColumns} rows={routeRows} />
            </div>
          </div>
        </CardContent>
      </Card>

      <section className="grid gap-6 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Component preview</CardTitle>
            <CardDescription>Quick check for common controls in this theme.</CardDescription>
          </CardHeader>
          <CardContent className="grid gap-4">
            <div className="flex flex-wrap gap-2">
              <Button>Primary</Button>
              <Button variant="outline">Outline</Button>
              <Button variant="secondary">Secondary</Button>
              <Button variant="danger">Danger</Button>
              <Button variant="ghost">Ghost</Button>
            </div>
            <div className="grid gap-3 md:grid-cols-2">
              <Input label="Input" placeholder="Type something" />
              <Select
                label="Select"
                options={[
                  { label: "Dashboard", value: "dashboard" },
                  { label: "Users", value: "users" },
                  { label: "Reports", value: "reports" }
                ]}
              />
            </div>
            <div className="flex flex-wrap gap-2">
              <Badge>Default</Badge>
              <Badge variant="info">Info</Badge>
              <Badge variant="success">Success</Badge>
              <Badge variant="warning">Warning</Badge>
              <Badge variant="danger">Danger</Badge>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Modal preview</CardTitle>
            <CardDescription>Use the modal component for forms and confirmation flows.</CardDescription>
          </CardHeader>
          <CardContent>
            <Button onClick={() => setModalOpen(true)}>
              Open modal
              <ExternalLink className="size-4" />
            </Button>
          </CardContent>
        </Card>
      </section>

      <Modal
        open={modalOpen}
        onClose={() => setModalOpen(false)}
        title="Theme modal"
        description="This modal uses the same card, border, button, and input tokens as the rest of the app."
        footer={
          <>
            <Button variant="outline" onClick={() => setModalOpen(false)}>
              Cancel
            </Button>
            <Button onClick={() => setModalOpen(false)}>Looks good</Button>
          </>
        }
      >
        <Input label="Sample field" placeholder="Theme preview" />
      </Modal>
    </div>
  );
}
