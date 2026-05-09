import { Mail, MapPin, ShieldCheck } from "lucide-react";
import { Badge } from "../components/ui/Badge";
import { Card } from "../components/ui/Card";
import { Button } from "../components/ui/Button";

export function Profile() {
  return (
    <div className="grid gap-6">
      <Card className="overflow-hidden">
        <div className="h-28 bg-gradient-to-r from-primary/30 via-indigo-soft to-amber-soft" />
        <div className="px-6 pb-6">
          <div className="-mt-10 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
            <div className="flex items-end gap-4">
              <span className="grid size-20 place-items-center rounded-2xl border-4 border-card bg-primary text-2xl font-extrabold text-primary-foreground">
                AC
              </span>
              <div className="pb-1">
                <h1 className="text-2xl font-bold text-foreground">Admin Console</h1>
                <p className="text-muted-foreground">Platform administrator</p>
              </div>
            </div>
            <Button variant="outline">Edit profile</Button>
          </div>

          <div className="mt-6 grid gap-4 md:grid-cols-3">
            {[
              { label: "Email", value: "admin@company.test", icon: Mail },
              { label: "Location", value: "Toronto, Canada", icon: MapPin },
              { label: "Access", value: "Owner", icon: ShieldCheck }
            ].map((item) => {
              const Icon = item.icon;

              return (
                <div className="rounded-xl border border-border bg-muted/55 p-4" key={item.label}>
                  <Icon className="size-5 text-primary" />
                  <p className="mt-3 text-sm font-bold text-muted-foreground">{item.label}</p>
                  <p className="mt-1 font-bold text-foreground">{item.value}</p>
                </div>
              );
            })}
          </div>

          <div className="mt-6 flex flex-wrap gap-2">
            <Badge variant="success">MFA enabled</Badge>
            <Badge variant="info">Admin</Badge>
            <Badge>Last login today</Badge>
          </div>
        </div>
      </Card>
    </div>
  );
}
