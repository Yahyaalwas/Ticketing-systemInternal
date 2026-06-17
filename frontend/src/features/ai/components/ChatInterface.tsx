import {
  Box, TextField, IconButton, Typography, Paper, CircularProgress,
  Avatar, Chip, Button,
} from '@mui/material';
import { Send as SendIcon, AutoAwesome as AiIcon, Delete as ClearIcon } from '@mui/icons-material';
import { useState, useRef, useEffect } from 'react';
import { useMutation } from '@tanstack/react-query';
import { useAiStore } from '@/stores/aiStore';
import { askKnowledgeQuestion, naturalLanguageSearch } from '@/api/ai';
import { formatDistanceToNow } from '@/utils/date';

const SUGGESTED_PROMPTS = [
  'What high priority tickets are overdue?',
  'Show unassigned critical bugs',
  'What happened with payment gateway issues?',
  'Which tickets are at risk of SLA breach?',
  'Show bugs from last week',
];

export function ChatInterface() {
  const [input, setInput] = useState('');
  const { messages, isThinking, addMessage, setThinking, clearMessages } = useAiStore();
  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, isThinking]);

  const searchMutation = useMutation({
    mutationFn: (query: string) => naturalLanguageSearch(query),
    onSuccess: (data) => {
      addMessage({
        role: 'assistant',
        content: `**${data.interpretation}**\n\nFound ${data.tickets.length} ticket(s):\n${data.tickets.slice(0, 8).map(t => `- **${t.ticketKey}**: ${t.title} (${t.statusName})`).join('\n')}`,
      });
      setThinking(false);
    },
    onError: (err: Error) => {
      addMessage({ role: 'assistant', content: `I encountered an error: ${err.message}` });
      setThinking(false);
    },
  });

  const knowledgeMutation = useMutation({
    mutationFn: (question: string) => askKnowledgeQuestion(question),
    onSuccess: (data) => {
      addMessage({
        role: 'assistant',
        content: data.answer,
        citations: data.citations.map(c => ({ ticketKey: c.ticketKey, title: c.title, relevance: c.relevance })),
      });
      setThinking(false);
    },
    onError: (err: Error) => {
      addMessage({ role: 'assistant', content: `I encountered an error: ${err.message}` });
      setThinking(false);
    },
  });

  const handleSend = (text = input.trim()) => {
    if (!text) return;
    setInput('');
    addMessage({ role: 'user', content: text });
    setThinking(true);

    // Route to appropriate handler based on intent
    const lower = text.toLowerCase();
    if (lower.includes('show') || lower.includes('list') || lower.includes('find') ||
        lower.includes('assigned') || lower.includes('overdue') || lower.includes('priority')) {
      searchMutation.mutate(text);
    } else {
      knowledgeMutation.mutate(text);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); handleSend(); }
  };

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%', overflow: 'hidden' }}>
      {/* Messages */}
      <Box sx={{ flex: 1, overflow: 'auto', p: 2, display: 'flex', flexDirection: 'column', gap: 1.5 }}>
        {messages.length === 0 && (
          <Box sx={{ textAlign: 'center', pt: 4 }}>
            <AiIcon sx={{ fontSize: 48, color: 'primary.light', mb: 1 }} />
            <Typography variant="body2" color="text.secondary" mb={2}>
              Ask me about your tickets, search by natural language, or get insights about your project.
            </Typography>
            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.75, justifyContent: 'center' }}>
              {SUGGESTED_PROMPTS.map((p) => (
                <Chip key={p} label={p} size="small" onClick={() => handleSend(p)}
                  sx={{ cursor: 'pointer', '&:hover': { bgcolor: 'primary.light', color: 'primary.contrastText' } }} />
              ))}
            </Box>
          </Box>
        )}

        {messages.map((msg) => (
          <Box
            key={msg.id}
            sx={{
              display: 'flex',
              flexDirection: msg.role === 'user' ? 'row-reverse' : 'row',
              gap: 1, alignItems: 'flex-start',
            }}
          >
            <Avatar
              sx={{
                width: 28, height: 28, flexShrink: 0,
                bgcolor: msg.role === 'user' ? 'primary.main' : 'secondary.main',
                fontSize: 12,
              }}
            >
              {msg.role === 'user' ? 'U' : <AiIcon sx={{ fontSize: 16 }} />}
            </Avatar>
            <Box sx={{ maxWidth: '80%' }}>
              <Paper
                elevation={0}
                sx={{
                  p: 1.5, borderRadius: 2,
                  bgcolor: msg.role === 'user' ? 'primary.main' : 'action.hover',
                  color: msg.role === 'user' ? 'primary.contrastText' : 'text.primary',
                }}
              >
                <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap', lineHeight: 1.6 }}>
                  {msg.content}
                </Typography>
              </Paper>
              {msg.citations && msg.citations.length > 0 && (
                <Box sx={{ mt: 0.5, display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                  {msg.citations.map((c) => (
                    <Chip key={c.ticketKey} label={c.ticketKey} size="small" variant="outlined"
                      color="primary" title={c.relevance} />
                  ))}
                </Box>
              )}
              <Typography variant="caption" color="text.disabled" sx={{ px: 0.5 }}>
                {formatDistanceToNow(msg.timestamp)}
              </Typography>
            </Box>
          </Box>
        ))}

        {isThinking && (
          <Box sx={{ display: 'flex', gap: 1, alignItems: 'center' }}>
            <Avatar sx={{ width: 28, height: 28, bgcolor: 'secondary.main' }}>
              <AiIcon sx={{ fontSize: 16 }} />
            </Avatar>
            <Paper elevation={0} sx={{ p: 1.5, borderRadius: 2, bgcolor: 'action.hover' }}>
              <CircularProgress size={16} />
            </Paper>
          </Box>
        )}
        <div ref={bottomRef} />
      </Box>

      {/* Clear + Input */}
      {messages.length > 0 && (
        <Box sx={{ px: 2, pb: 0.5 }}>
          <Button size="small" startIcon={<ClearIcon />} onClick={clearMessages} color="inherit">
            Clear
          </Button>
        </Box>
      )}
      <Box sx={{ p: 2, borderTop: 1, borderColor: 'divider', display: 'flex', gap: 1 }}>
        <TextField
          fullWidth
          multiline
          maxRows={3}
          size="small"
          placeholder="Ask about tickets, search, or get insights..."
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={handleKeyDown}
          disabled={isThinking}
        />
        <IconButton color="primary" onClick={() => handleSend()} disabled={!input.trim() || isThinking}>
          <SendIcon />
        </IconButton>
      </Box>
    </Box>
  );
}
