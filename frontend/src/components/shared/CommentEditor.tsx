import {
  Box, TextField, Button, IconButton, Tooltip, Menu, MenuItem,
  Paper, Typography, CircularProgress, Divider,
} from '@mui/material';
import {
  FormatBold, FormatItalic, Code, Link, FormatListBulleted,
  FormatListNumbered, FormatQuote, AutoAwesome as AiIcon, Send as SendIcon,
} from '@mui/icons-material';
import { useRef, useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { generateComment } from '@/api/ai';

interface ToolbarButton {
  icon: React.ReactNode;
  title: string;
  prefix: string;
  suffix: string;
  block?: boolean;
}

const TOOLBAR: ToolbarButton[] = [
  { icon: <FormatBold fontSize="small" />, title: 'Bold', prefix: '**', suffix: '**' },
  { icon: <FormatItalic fontSize="small" />, title: 'Italic', prefix: '_', suffix: '_' },
  { icon: <Code fontSize="small" />, title: 'Inline code', prefix: '`', suffix: '`' },
  { icon: <Link fontSize="small" />, title: 'Link', prefix: '[', suffix: '](url)' },
  { icon: <FormatListBulleted fontSize="small" />, title: 'Bullet list', prefix: '\n- ', suffix: '', block: true },
  { icon: <FormatListNumbered fontSize="small" />, title: 'Numbered list', prefix: '\n1. ', suffix: '', block: true },
  { icon: <FormatQuote fontSize="small" />, title: 'Quote', prefix: '\n> ', suffix: '', block: true },
];

const AI_OPTIONS = [
  { label: '✨ Improve Writing', instruction: 'Improve the grammar, clarity, and flow of this text', tone: 'Professional' },
  { label: '👔 Make Professional', instruction: 'Rewrite this in a polished professional tone', tone: 'Professional' },
  { label: '⚙️ Technical Detail', instruction: 'Add technical precision and detail', tone: 'Technical' },
  { label: '📣 Customer Facing', instruction: 'Rewrite for clear customer communication, no jargon', tone: 'CustomerFacing' },
  { label: '📋 Status Update', instruction: 'Format as a concise status update', tone: 'StatusUpdate' },
  { label: '✂️ Shorten', instruction: 'Shorten while preserving all key points', tone: 'Professional' },
  { label: '📝 Expand', instruction: 'Expand with more detail and context', tone: 'Professional' },
];

interface CommentEditorProps {
  ticketId: string;
  initialValue?: string;
  placeholder?: string;
  submitLabel?: string;
  onSubmit: (body: string) => Promise<void>;
  onCancel?: () => void;
  loading?: boolean;
}

export function CommentEditor({
  ticketId,
  initialValue = '',
  placeholder = 'Write a comment... (Markdown supported)',
  submitLabel = 'Add Comment',
  onSubmit,
  onCancel,
  loading = false,
}: CommentEditorProps) {
  const [value, setValue] = useState(initialValue);
  const [aiMenuAnchor, setAiMenuAnchor] = useState<null | HTMLElement>(null);
  const [aiPreview, setAiPreview] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  const aiMutation = useMutation({
    mutationFn: ({ instruction, tone }: { instruction: string; tone: string }) => {
      const fullInstruction = value.trim()
        ? `User has written: "${value.trim()}". ${instruction}`
        : instruction;
      return generateComment(ticketId, fullInstruction, tone);
    },
    onSuccess: (data) => setAiPreview(data.draftBody),
  });

  const wrapSelection = (prefix: string, suffix: string) => {
    const ta = textareaRef.current;
    if (!ta) return;
    const start = ta.selectionStart;
    const end = ta.selectionEnd;
    const selected = value.slice(start, end);
    const newValue = value.slice(0, start) + prefix + (selected || 'text') + suffix + value.slice(end);
    setValue(newValue);
    requestAnimationFrame(() => {
      ta.focus();
      const newPos = start + prefix.length;
      ta.setSelectionRange(newPos, newPos + (selected || 'text').length);
    });
  };

  const handleSubmit = async () => {
    if (!value.trim()) return;
    setIsSubmitting(true);
    try {
      await onSubmit(value.trim());
      setValue('');
      setAiPreview(null);
    } finally {
      setIsSubmitting(false);
    }
  };

  const useAiText = () => {
    if (aiPreview) { setValue(aiPreview); setAiPreview(null); }
  };

  return (
    <Box>
      {/* Toolbar */}
      <Box sx={{
        display: 'flex', alignItems: 'center', gap: 0.25, px: 1, py: 0.5,
        bgcolor: 'action.hover', borderRadius: '8px 8px 0 0',
        border: '1px solid', borderColor: 'divider', borderBottom: 'none',
        flexWrap: 'wrap',
      }}>
        {TOOLBAR.map((btn) => (
          <Tooltip key={btn.title} title={btn.title}>
            <IconButton size="small" onClick={() => wrapSelection(btn.prefix, btn.suffix)}>
              {btn.icon}
            </IconButton>
          </Tooltip>
        ))}
        <Box sx={{ ml: 'auto' }}>
          <Tooltip title="AI Writing Assistant">
            <IconButton
              size="small"
              color="primary"
              onClick={(e) => setAiMenuAnchor(e.currentTarget)}
              disabled={aiMutation.isPending}
            >
              {aiMutation.isPending ? <CircularProgress size={16} /> : <AiIcon fontSize="small" />}
            </IconButton>
          </Tooltip>
        </Box>
      </Box>

      {/* Textarea */}
      <TextField
        multiline
        minRows={4}
        maxRows={12}
        fullWidth
        placeholder={placeholder}
        value={value}
        onChange={(e) => setValue(e.target.value)}
        inputRef={textareaRef}
        onKeyDown={(e) => {
          if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) handleSubmit();
        }}
        sx={{
          '& .MuiOutlinedInput-root': {
            borderRadius: '0 0 8px 8px',
            fontFamily: '"JetBrains Mono", monospace',
            fontSize: '0.875rem',
          },
        }}
      />

      {/* AI Preview */}
      {aiPreview && (
        <Paper variant="outlined" sx={{ mt: 1, p: 1.5, bgcolor: 'action.hover', borderColor: 'primary.light' }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, mb: 1 }}>
            <AiIcon color="primary" fontSize="small" />
            <Typography variant="caption" fontWeight={600} color="primary">AI Suggestion</Typography>
            <Typography variant="caption" color="text.secondary">(Review before using)</Typography>
          </Box>
          <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap', mb: 1.5 }}>{aiPreview}</Typography>
          <Box sx={{ display: 'flex', gap: 1 }}>
            <Button size="small" variant="contained" onClick={useAiText}>Use This</Button>
            <Button size="small" onClick={() => setAiPreview(null)}>Discard</Button>
          </Box>
        </Paper>
      )}

      {/* Actions */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mt: 1 }}>
        <Typography variant="caption" color="text.disabled">Ctrl+Enter to submit · Markdown supported</Typography>
        <Box sx={{ display: 'flex', gap: 1 }}>
          {onCancel && <Button size="small" onClick={onCancel}>Cancel</Button>}
          <Button
            size="small"
            variant="contained"
            startIcon={(isSubmitting || loading) ? <CircularProgress size={14} color="inherit" /> : <SendIcon fontSize="small" />}
            disabled={!value.trim() || isSubmitting || loading}
            onClick={handleSubmit}
          >
            {submitLabel}
          </Button>
        </Box>
      </Box>

      {/* AI Menu */}
      <Menu anchorEl={aiMenuAnchor} open={Boolean(aiMenuAnchor)} onClose={() => setAiMenuAnchor(null)}>
        {AI_OPTIONS.map((opt) => (
          <MenuItem
            key={opt.label}
            dense
            onClick={() => {
              setAiMenuAnchor(null);
              aiMutation.mutate({ instruction: opt.instruction, tone: opt.tone });
            }}
          >
            <Typography variant="body2">{opt.label}</Typography>
          </MenuItem>
        ))}
      </Menu>
    </Box>
  );
}
