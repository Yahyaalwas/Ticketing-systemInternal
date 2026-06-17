import { Box, Typography, Card, CardContent, List, ListItem, ListItemText, Divider } from '@mui/material';

export default function AdminPage() {
  return (
    <Box>
      <Typography variant="h5" fontWeight={700} mb={3}>Administration</Typography>
      <Card sx={{ maxWidth: 600 }}>
        <CardContent>
          <Typography variant="subtitle1" fontWeight={600} gutterBottom>System Settings</Typography>
          <Divider sx={{ mb: 2 }} />
          <List disablePadding>
            {['User Management', 'Role & Permission Mapping', 'Workflow Configuration', 'SLA Policies', 'Email Notifications', 'Audit Log'].map((item) => (
              <ListItem key={item} sx={{ px: 0, py: 1, borderBottom: '1px solid', borderColor: 'divider' }}>
                <ListItemText primary={item} />
              </ListItem>
            ))}
          </List>
        </CardContent>
      </Card>
    </Box>
  );
}
