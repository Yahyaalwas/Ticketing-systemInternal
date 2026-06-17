import {
  Drawer, Box, Tabs, Tab, IconButton, Typography, Tooltip,
  Chip,
} from '@mui/material';
import {
  Close as CloseIcon,
  Chat as ChatIcon,
  Summarize as SummarizeIcon,
  Search as SearchIcon,
  Notes as MeetingIcon,
  Assessment as ReportIcon,
  Article as DraftIcon,
} from '@mui/icons-material';
import { useAiStore } from '@/stores/aiStore';
import { ChatInterface } from './components/ChatInterface';
import { NaturalLanguageSearch } from './components/NaturalLanguageSearch';
import { MeetingParser } from './components/MeetingParser';
import { ExecutiveReportGenerator } from './components/ExecutiveReportGenerator';
import { TicketDrafter } from './components/TicketDrafter';

const PANEL_WIDTH = 480;

const TABS = [
  { value: 'chat', label: 'Chat', icon: <ChatIcon fontSize="small" /> },
  { value: 'search', label: 'Search', icon: <SearchIcon fontSize="small" /> },
  { value: 'meeting', label: 'Meeting', icon: <MeetingIcon fontSize="small" /> },
  { value: 'report', label: 'Report', icon: <ReportIcon fontSize="small" /> },
  { value: 'draft', label: 'Draft', icon: <DraftIcon fontSize="small" /> },
] as const;

export function AiAssistantPanel() {
  const { isOpen, activeTab, setTab, closePanel } = useAiStore();

  return (
    <Drawer
      anchor="right"
      open={isOpen}
      onClose={closePanel}
      variant="persistent"
      sx={{
        width: isOpen ? PANEL_WIDTH : 0,
        flexShrink: 0,
        '& .MuiDrawer-paper': {
          width: PANEL_WIDTH,
          boxSizing: 'border-box',
          display: 'flex',
          flexDirection: 'column',
          border: 'none',
          boxShadow: '-4px 0 24px rgba(0,0,0,0.12)',
        },
      }}
    >
      {/* Header */}
      <Box
        sx={{
          px: 2, py: 1.5,
          display: 'flex', alignItems: 'center', justifyContent: 'space-between',
          bgcolor: 'primary.main', color: 'primary.contrastText',
        }}
      >
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <Typography variant="subtitle1" fontWeight={700}>AI Assistant</Typography>
          <Chip label="Beta" size="small" sx={{ bgcolor: 'rgba(255,255,255,0.2)', color: 'inherit', height: 18, fontSize: 10 }} />
        </Box>
        <IconButton size="small" onClick={closePanel} sx={{ color: 'inherit' }}>
          <CloseIcon />
        </IconButton>
      </Box>

      {/* Tabs */}
      <Box sx={{ borderBottom: 1, borderColor: 'divider', bgcolor: 'background.paper' }}>
        <Tabs
          value={activeTab}
          onChange={(_, v) => setTab(v)}
          variant="scrollable"
          scrollButtons="auto"
          sx={{ '& .MuiTab-root': { minWidth: 0, px: 1.5, py: 1, fontSize: 12 } }}
        >
          {TABS.map((t) => (
            <Tab key={t.value} value={t.value} label={t.label} icon={t.icon} iconPosition="start" />
          ))}
        </Tabs>
      </Box>

      {/* Content */}
      <Box sx={{ flex: 1, overflow: 'hidden', display: 'flex', flexDirection: 'column' }}>
        {activeTab === 'chat' && <ChatInterface />}
        {activeTab === 'search' && <NaturalLanguageSearch />}
        {activeTab === 'meeting' && <MeetingParser />}
        {activeTab === 'report' && <ExecutiveReportGenerator />}
        {activeTab === 'draft' && <TicketDrafter />}
      </Box>
    </Drawer>
  );
}
