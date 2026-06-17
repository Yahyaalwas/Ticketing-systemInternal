import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { CurrentUser } from '@/types';

interface AuthState {
  token: string | null;
  user: CurrentUser | null;
  isAuthenticated: boolean;
  setAuth: (token: string, user: CurrentUser) => void;
  logout: () => void;
  updateUser: (user: CurrentUser) => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      token: null,
      user: null,
      isAuthenticated: false,
      setAuth: (token, user) => set({ token, user, isAuthenticated: true }),
      logout: () => set({ token: null, user: null, isAuthenticated: false }),
      updateUser: (user) => set({ user }),
    }),
    { name: 'its-auth' }
  )
);
