import { addressSubfields, type FormAddressValue, type FormRecordValue } from "./types";

export function isFormAddressValue(value: FormRecordValue | undefined): value is FormAddressValue {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

export function formatAddressValue(value: FormAddressValue): string {
  const textParts = [value.line1, value.line2, value.city, value.region, value.postalCode, value.country]
    .filter((part): part is string => typeof part === "string" && part.trim().length > 0)
    .map((part) => part.trim());
  if (textParts.length > 0) return textParts.join(", ");
  const coordinates = [value.latitude, value.longitude].filter((part) => typeof part === "number" && Number.isFinite(part));
  return coordinates.length === 2 ? `${coordinates[0]}, ${coordinates[1]}` : "";
}

export function formatFormRecordValue(value: FormRecordValue | undefined, emptyValue = "-"): string {
  if (value === undefined || value === null || value === "") return emptyValue;
  if (typeof value === "boolean") return value ? "Yes" : "No";
  if (isFormAddressValue(value)) return formatAddressValue(value) || emptyValue;
  return String(value);
}

export function normalizeAddressValue(value: FormRecordValue | undefined): FormAddressValue {
  if (!isFormAddressValue(value)) return {};
  return Object.fromEntries(addressSubfields.filter((subfield) => value[subfield] !== undefined).map((subfield) => [subfield, value[subfield]]));
}
