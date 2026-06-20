import type { Metadata } from "next";
import "./styles.css";

export const metadata: Metadata = {
  title: "Pod-o-Sphere",
  description: "Turn your podcast archive into a searchable topic universe."
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <html lang="en"><body>{children}</body></html>;
}

