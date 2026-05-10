import { Lock, Mail, User } from "lucide-react";
import { AuthCard } from "../components/AuthCard";
import { Button } from "../../components/ui/Button";
import { Checkbox } from "../../components/ui/Checkbox";
import { Input } from "../../components/ui/Input";

export function ThemeRegister() {
  return (
    <AuthCard title="Create your workspace account" description="A registration template for invited internal users and workspace administrators.">
      <div className="grid gap-4">
        <Input label="Full name" placeholder="Jane Cooper" icon={<User size={16} />} />
        <Input label="Email" type="email" placeholder="jane@company.test" icon={<Mail size={16} />} />
        <Input label="Password" type="password" placeholder="Create a password" icon={<Lock size={16} />} />
        <Checkbox label="I agree to the workspace access policy" className="border-0 bg-transparent p-0" />
        <Button className="w-full justify-center">Create account</Button>
      </div>
    </AuthCard>
  );
}
