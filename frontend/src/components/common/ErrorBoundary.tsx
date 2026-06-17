import React from 'react';
import { Box, Button, Typography, Container } from '@mui/material';
import ErrorOutlineIcon from '@mui/icons-material/ErrorOutline';

interface State { hasError: boolean; error?: Error; }

export class ErrorBoundary extends React.Component<{ children: React.ReactNode }, State> {
  constructor(props: { children: React.ReactNode }) {
    super(props);
    this.state = { hasError: false };
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  render() {
    if (this.state.hasError) {
      return (
        <Container maxWidth="sm" sx={{ mt: 8, textAlign: 'center' }}>
          <ErrorOutlineIcon sx={{ fontSize: 64, color: 'error.main', mb: 2 }} />
          <Typography variant="h5" gutterBottom>Something went wrong</Typography>
          <Typography color="text.secondary" sx={{ mb: 3 }}>{this.state.error?.message}</Typography>
          <Button variant="contained" onClick={() => window.location.reload()}>Reload Page</Button>
        </Container>
      );
    }
    return this.props.children;
  }
}
