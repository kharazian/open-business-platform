import { Lock, Mail } from "lucide-react";
import { cn } from "../../lib/cn";
import { Checkbox } from "../../components/ui/Checkbox";
import { Button } from "../../components/ui/Button";
import { Card } from "../../components/ui/Card";
import { Input } from "../../components/ui/Input";
import { useThemeAppearance } from "../../context/ThemeAppearanceContext";

export function ThemeLogin() {
  const { palette } = useThemeAppearance();

  return (
    <div className="mx-auto flex min-h-[70vh] w-full max-w-md items-center">
      <Card>
        <div className="mb-8 text-center">
          <div className={cn("mx-auto flex size-12 items-center justify-center rounded-2xl text-lg font-black text-white", palette.primaryBg)}>OB</div>
          <h1 className="mt-5 text-2xl font-bold text-foreground">Sign in to your workspace</h1>
          <p className="mt-2 text-sm text-muted-foreground">A clean auth screen that still lives inside the theme playground.</p>
        </div>

        <div className="grid gap-4">
          <Input label="Email" type="email" placeholder="you@company.com" icon={<Mail size={16} />} />
          <Input label="Password" type="password" placeholder="Enter your password" icon={<Lock size={16} />} />
          <div className="flex items-center justify-between text-sm">
            <Checkbox label="Remember me" className="border-0 bg-transparent p-0" />
            <button className={cn("font-medium hover:underline", palette.primaryText)} type="button">Forgot password?</button>
          </div>
          <Button className="w-full justify-center">Sign in</Button>
        </div>
      </Card>
    </div>
  );
}
