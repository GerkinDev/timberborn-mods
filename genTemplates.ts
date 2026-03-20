import { expandGlob } from "jsr:@std/fs@1.0.23";
import { dirname, resolve } from "node:path";
import { JsonValue, parse } from "jsr:@std/jsonc@1.0.2";
import assert from "node:assert";
import { isAbsolute } from "node:path";
import { format } from "node:util";

const cache = new Map<string, Extract<JsonValue, { [key: string]: unknown }>>();

function isRecord<T>(
  value: T,
): value is Extract<T, { [key: string]: unknown }> {
  return typeof value === "object" && !Array.isArray(value) &&
    value !== null;
}
const sortObjectDeep = <T>(obj: T): T => {
  if (Array.isArray(obj)) {
    return obj.map(sortObjectDeep) as T;
  }
  if (isRecord(obj)) {
    return Object.fromEntries(
      Object.entries(obj).sort(([a], [b]) => a.localeCompare(b)).map((
        [k, v],
      ) => [k, sortObjectDeep(v)]),
    ) as T;
  }
  return obj;
};

const mergeObjects = <T>(path: PropertyKey[], a?: T, b?: T): T => {
  if (a === undefined || a === null) {
    return b as T;
  }
  if (b === undefined || b === null) {
    return a as T;
  }
  if (typeof a === "object" && typeof b === "object") {
    const keys = [...new Set([...Object.keys(a), ...Object.keys(b)])];
    const ret = {} as Record<string, unknown>;
    for (const key of keys) {
      ret[key] = mergeObjects(
        [...path, key],
        a[key as keyof T],
        b[key as keyof T],
      );
    }
    return ret as T;
  }
  throw new Error(`Failed to merge ${Deno.inspect(a)} & ${Deno.inspect(b)}`);
};
const resolveTemplate = async (path: string, stack: string[]) => {
  try {
    assert(isAbsolute(path));
    if (cache.has(path)) {
      return cache.get(path)!;
    }
    const content = await Deno.readTextFile(path);
    const parsed = parse(content);
    assert(isRecord(parsed));
    let final = parsed;
    if ("@extends" in final) {
      assert(typeof final["@extends"] === "string");
      const extended = resolve(dirname(path), final["@extends"]);
      delete final["@extends"];
      assert(!stack.includes(extended));
      const extendedContent = structuredClone(
        await resolveTemplate(extended, [...stack, path]),
      );
      const merged = mergeObjects([], extendedContent, final);
      assert(isRecord(merged));
      final = merged;
    }
    if ("@output" in final) {
      assert(typeof final["@output"] === "string");
      const output = resolve(dirname(path), final["@output"]);
      delete final["@output"];
      await Deno.writeTextFile(
        output,
        JSON.stringify(sortObjectDeep(final), null, 2) + "\n",
      );
    }
    cache.set(path, final);
    return final;
  } catch (error) {
    throw new Error(
      format("Failed to handle template at %s, stack %o", path, stack),
      { cause: error },
    );
  }
};

for await (
  const template of expandGlob("**/*.genTemplate.json{,c,5}", {
    root: resolve(import.meta.dirname!, "./Assets"),
  })
) {
  if (!template.isFile) {
    continue;
  }
  await resolveTemplate(template.path, []);
}
