import { Lock, Mail } from "lucide-react";
import { cn } from "../../lib/cn";
import { AuthCard } from "../components/AuthCard";
import { Checkbox } from "../../components/ui/Checkbox";
import { Button } from "../../components/ui/Button";
import { Input } from "../../components/ui/Input";
import { useThemeAppearance } from "../../context/ThemeAppearanceContext";

export function ThemeLogin() {
  const { palette } = useThemeAppearance();

  return (
    <AuthCard title="Sign in to your workspace" description="A clean auth screen that still lives inside the theme playground.">
      <div className="grid gap-4">
        <Input label="Email" type="email" placeholder="you@company.com" icon={<Mail size={16} />} />
        <Input label="Password" type="password" placeholder="Enter your password" icon={<Lock size={16} />} />
        <div className="flex items-center justify-between text-sm">
          <Checkbox label="Remember me" className="border-0 bg-transparent p-0" />
          <button className={cn("font-medium hover:underline", palette.primaryText)} type="button">Forgot password?</button>
        </div>
        <Button className="w-full justify-center">Sign in</Button>
      </div>
    </AuthCard>
  );
}
