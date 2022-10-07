export interface Party {
  id: string;
  name: string;
  worldId: number;
  job: number;
  inParty: boolean;
}

declare global {
  namespace OverlayPlugin {
    function addOverlayListener(event: 'LogLine', cb: (ev: { type: 'LogLine'; line: string[]; rawLine: string; }) => void): void;
    function addOverlayListener(event: 'PartyChanged', cb: (ev: { type: 'PartyChanged'; party: Party[]; }) => void): void;

    function callOverlayHandler(msg: { call: 'saveData'; key: string; data: unknown }): Promise<void>;
    function callOverlayHandler<T = unknown>(msg: { call: 'loadData'; key: string; }): Promise<({ data?: T } | undefined)>;
  }

  const callOverlayHandler: typeof OverlayPlugin.callOverlayHandler;
  const addOverlayListener: typeof OverlayPlugin.addOverlayListener;
}

export {};
