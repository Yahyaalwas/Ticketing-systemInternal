import { create } from 'zustand';

export interface ChatMessage {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  timestamp: Date;
  citations?: Array<{ ticketKey: string; title: string; relevance: string }>;
}

type AiTab = 'chat' | 'summarize' | 'search' | 'meeting' | 'report' | 'draft';

interface AiState {
  isOpen: boolean;
  activeTab: AiTab;
  messages: ChatMessage[];
  isThinking: boolean;
  togglePanel: () => void;
  openPanel: (tab?: AiTab) => void;
  closePanel: () => void;
  setTab: (tab: AiTab) => void;
  addMessage: (msg: Omit<ChatMessage, 'id' | 'timestamp'>) => void;
  setThinking: (val: boolean) => void;
  clearMessages: () => void;
}

export const useAiStore = create<AiState>((set) => ({
  isOpen: false,
  activeTab: 'chat',
  messages: [],
  isThinking: false,
  togglePanel: () => set((s) => ({ isOpen: !s.isOpen })),
  openPanel: (tab) => set({ isOpen: true, ...(tab ? { activeTab: tab } : {}) }),
  closePanel: () => set({ isOpen: false }),
  setTab: (tab) => set({ activeTab: tab }),
  addMessage: (msg) =>
    set((s) => ({
      messages: [...s.messages, { ...msg, id: crypto.randomUUID(), timestamp: new Date() }],
    })),
  setThinking: (val) => set({ isThinking: val }),
  clearMessages: () => set({ messages: [] }),
}));
