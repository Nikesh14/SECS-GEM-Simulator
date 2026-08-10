# SECS/GEM — Implemented Messages Cheatsheet

A living reference of every SECS-II Stream/Function this simulator handles.
_Last updated: 2026-08-08_

**How to read a name:** `SxFy` = Stream x, Function y. **Odd function = primary (request), even function = reply.**
**Dir:** `H→E` host→equipment, `E→H` equipment→host. **W** = wait-bit set (reply expected).

---

## Stream 1 — Equipment Status & Communication

| Msg | Name | Dir | W | Body | Reply | Notes |
|-----|------|-----|---|------|-------|-------|
| **S1F1**  | Are You There        | H→E | ✔ | _(none)_                         | S1F2  | |
| **S1F2**  | On-Line Data         | E→H | — | `L,2 { MDLN, SOFTREV }`          | —     | Equipment identity. Body with S1F1 present → S9F7. |
| **S1F13** | Establish Comms Req  | H→E | ✔ | `L,2 { MDLN, SOFTREV }`          | S1F14 | Drives **CommunicationState**. |
| **S1F14** | Establish Comms Ack  | E→H | — | `L,2 { COMMACK, L,2{MDLN,SOFTREV} }` | — | `COMMACK` 0=Accepted / 1=Denied. Denied unless enabled + valid + device-id match. On accept: `NotCommunicating → Communicating`. |
| **S1F15** | Request OFFLINE      | H→E | ✔ | _(none)_                         | S1F16 | Requires `Communicating`. |
| **S1F16** | Offline Ack          | E→H | — | `B OFLACK`                       | —     | `OFLACK` 0=Accepted / 1=Denied. On accept: control → `Offline`. |
| **S1F17** | Request ONLINE       | H→E | ✔ | _(none)_                         | S1F18 | Requires `Communicating`. |
| **S1F18** | Online Ack           | E→H | — | `B ONLACK`                       | —     | `ONLACK` 0=Accepted / 1=Denied. On accept: `Offline → Online`, substate from `DefaultOnlineState` config. |

## Stream 9 — System Errors (E→H, reply-less notifications)

| Msg | Name | Trigger | Body |
|-----|------|---------|------|
| **S9F3** | Unrecognized Stream   | Stream not supported at all           | `B[10]` MHEAD (offending 10-byte header) |
| **S9F5** | Unrecognized Function | Known stream, unhandled function      | `B[10]` MHEAD |
| **S9F7** | Illegal Data          | Recognized message, malformed payload | `B[10]` MHEAD |

---

## State models wired

- **CommunicationState** — `Disabled` / `NotCommunicating` / `Communicating`. Driven by **S1F13**. Starts `NotCommunicating`.
- **ControlState** — `Offline` / `OnlineLocal` / `OnlineRemote`. Driven by **S1F15** (→Offline) and **S1F17** (→Online). Starts `Offline`. Online substate chosen by the equipment (`DefaultOnlineState`), not the host.

## Infrastructure (not S/F, but supporting)

- HSMS control: Select / Deselect / Linktest / Separate + Reject.
- `SendAsync` (SystemBytes-correlated request/reply), SML formatter, `IDataMessageHandler` dispatch.

---

## Not yet implemented (roadmap)

| Msg | Purpose | Why next |
|-----|---------|----------|
| S1F3 / S1F4   | Selected Equipment Status Request | Lets host read status variables (incl. control state → resolves host `Pending`) |
| S2F41 / S2F42 | Host/Remote Command               | **First consumer of ControlState** — gated on `OnlineRemote` |
| S2F17 / S2F18 | Date/Time Request                 | Simple, good S2 warm-up |
| S2F13 / S2F14 | Equipment Constant Request        | Needs an EC registry |
| S6F11 / S6F12 | Event Report Send                 | Equipment→host events (needs working host handler + transaction manager) |
| S5F1 / S5F2   | Alarm Report                      | Alarm management |

_Tests: see `SecsGem.Core.Tests/HandlerTestCases` (per-message behavior) and `HsmsTestCases` (transport + end-to-end)._
