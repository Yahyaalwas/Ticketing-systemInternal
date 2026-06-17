import { useState, useCallback } from 'react';

interface Options {
  title?: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
}

export function useConfirmDialog() {
  const [open, setOpen] = useState(false);
  const [options, setOptions] = useState<Options>({ message: '' });
  const [resolve, setResolve] = useState<(value: boolean) => void>(() => () => {});

  const confirm = useCallback((opts: Options): Promise<boolean> => {
    setOptions(opts);
    setOpen(true);
    return new Promise((res) => setResolve(() => res));
  }, []);

  const handleClose = (value: boolean) => {
    setOpen(false);
    resolve(value);
  };

  return { open, options, confirm, handleClose };
}
