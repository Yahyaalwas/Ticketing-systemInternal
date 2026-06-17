import { useSnackbar } from 'notistack';

export function useNotification() {
  const { enqueueSnackbar } = useSnackbar();

  return {
    success: (msg: string) => enqueueSnackbar(msg, { variant: 'success' }),
    error: (msg: string) => enqueueSnackbar(msg, { variant: 'error' }),
    info: (msg: string) => enqueueSnackbar(msg, { variant: 'info' }),
    warning: (msg: string) => enqueueSnackbar(msg, { variant: 'warning' }),
  };
}
