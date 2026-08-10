# EquipmentSimulator — a SECS/GEM equipment & host simulator

A from-scratch implementation of the **SECS/GEM** protocol stack in **C# / .NET 8** — the language semiconductor equipment and factory hosts use to talk to each other on the fab floor.

> **Status: learning project, in progress.** This is built by hand, byte-up, to understand the protocol deeply — not a commercial GEM stack. It implements a **correct, wire-conformant subset** (communication + control-state handshakes) with a full test suite; the broader GEM services (data collection, events, alarms, remote commands) are on the roadmap. See [Conformance](#conformance) for exactly where it stands.

---

## What's in the box

Two console apps talking real HSMS over TCP/IP, built on one reusable protocol library:

| Project | Role |
|---|---|
| **`SecsGem.Core`** | The engine — SECS-II codec (SEMI E5), HSMS transport (SEMI E37), the HSMS session/state machine, and the GEM domain models. Config-agnostic and reusable. |
| **`EquipmentSimulator`** | The equipment (HSMS **passive** / server). Listens, answers the host, holds its own communication/control state. |
| **`HostSimulator`** | The factory host (HSMS **active** / client). Connects and drives the conversation. |
| **`SecsGem.Core.Tests`** | 118 tests — SECS-II codec (golden-vector), HSMS session, GEM handlers, and end-to-end socket flows. |

## Implemented so far

- **HSMS transport (E37):** framing, the Select / Linktest / Deselect / Separate handshake, SystemBytes transaction correlation.
- **SECS-II (E5):** full item model (List, ASCII, Binary, Boolean, I1–I8, U1–U8, F4/F8, JIS-8) with a conformant encoder/decoder, plus an SML formatter.
- **GEM (E30) messages:** `S1F1/F2` (Are You There / On-Line Data), `S1F13/F14` (Establish Communications), `S1F15/F16` (Request OFFLINE), `S1F17/F18` (Request ONLINE), and `S9F3/F5/F7` error messages.
- **GEM state models:** Communication State (Disabled / Not-Communicating / Communicating) and Control State (Offline / Online-Local / Online-Remote).

The live, message-by-message status lives in **[`SECS_CHEATSHEET.md`](SECS_CHEATSHEET.md)**.

---

## Getting started

### Prerequisites
- [.NET SDK 8.0+](https://dotnet.microsoft.com/download)

### Build & test
```bash
dotnet build EquipmentSimulator.slnx
dotnet test  EquipmentSimulator.slnx
```

### Run it

Start the **equipment** first (it listens), then the **host** (it connects). In two terminals:

```bash
# Terminal 1 — equipment (passive / server)
dotnet run --project EquipmentSimulator
```
```bash
# Terminal 2 — host (active / client)
dotnet run --project HostSimulator
```

Each app prompts to start (`Y`), then asks for an IP address — use `127.0.0.1` on one machine. The default port is **5000**. Once connected you'll see the Select handshake, then the host establishing communication (`S1F13/F14`) and driving the control state (`S1F17` online / `S1F15` offline).

### Configuration

The equipment's identity and startup policy come from [`EquipmentSimulator/appsettings.json`](EquipmentSimulator/appsettings.json):

```json
{
  "Equipment": {
    "ModelName": "Lam S320",
    "SoftwareRevision": "1.0.0",
    "Manufacturer": "LAM Research",
    "SerialNumber": "3697AD36JKL",
    "DeviceId": 1001,
    "DefaultOnlineState": "OnlineRemote"
  }
}
```

`DefaultOnlineState` decides whether the equipment comes up **Local** or **Remote** when it goes online (the host requests *online*; the equipment chooses the substate).

---

## How it works

```
Connect (TCP)  →  Select (HSMS session)  →  S1F13/F14 (establish comms)
              →  S1F17/F18 (go online)   →  … GEM data messages …  →  Separate
```

`SecsGem.Core` handles bytes → HSMS → SECS-II and then **delegates the meaning of each message** to a per-role handler (`IDataMessageHandler`) in the app layer, so the engine never hard-codes GEM logic. The full design — layered model, message flow, and state machines (with diagrams) — is in **[`ARCHITECTURE.md`](ARCHITECTURE.md)**.

---

## Testing

```bash
dotnet test EquipmentSimulator.slnx
```

Anything that touches the **wire** (SECS-II items, the HSMS header, S9 headers) is covered by **golden-vector tests** — assertions against hand-verified, SEMI-standard bytes rather than values captured from our own encoder. This is deliberate: a plain round-trip test passes whenever the encoder and decoder merely agree with *each other*, so it hides bugs they share. Golden vectors compare against the standard, so a non-conformant change fails loudly. See `ARCHITECTURE.md` §6a.

---

## Conformance

- **Wire format (E5 / E37): conformant** for every message implemented — SECS-II item encoding, the 10-byte HSMS header (W-bit, stream/function, SystemBytes), and S9 MHEADs — all pinned by golden tests.
- **GEM (E30): a correct subset, not yet compliant.** Establish Communications and the control-state model are in; data collection, event/report, alarms, remote commands, clock, spooling, and process programs are not built yet.
- **Known gaps (incomplete, not incorrect):** HSMS timers (T3–T8), Reject.req reason codes, and a single-transaction `SendAsync` (a transaction manager is planned for when the equipment starts initiating events).

## Roadmap

1. Status/data variables + `S1F3/F4` (data collection foundation)
2. Remote commands `S2F41/F42` (gated on Online-Remote)
3. Dynamic event reports `S2F33/35/37` + `S6F11`
4. Alarms (S5), clock (S2F17/18), and HSMS timers

---

## Standards referenced

- **SEMI E4** — SECS-I (serial; superseded here by HSMS)
- **SEMI E5** — SECS-II (message content & data items)
- **SEMI E37** — HSMS (SECS messaging over TCP/IP)
- **SEMI E30** — GEM (Generic Equipment Model)

---

*Built as a hands-on way to learn SECS/GEM end to end. Feedback from anyone working in equipment integration or smart manufacturing is very welcome.*
