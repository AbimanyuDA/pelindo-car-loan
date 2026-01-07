import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Play, CheckCircle } from 'lucide-react'
import { Button } from '@/components/ui/Button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card'
import { Table, Column } from '@/components/ui/Table'
import { Badge } from '@/components/ui/Badge'
import { PageLoading } from '@/components/ui/Loading'
import { Alert } from '@/components/ui/Alert'
import { scheduleService } from '@/services'
import { formatDate } from '@/lib/utils'
import type { DriverSchedule } from '@/types'

export default function DriverSchedulePage() {
  const queryClient = useQueryClient()

  const { data: schedules, isLoading, error } = useQuery({
    queryKey: ['my-schedules'],
    queryFn: async () => {
      const response = await scheduleService.getMySchedules()
      return response.data || []
    }
  })

  const updateStatusMutation = useMutation({
    mutationFn: async ({ id, status }: { id: number; status: string }) => {
      return scheduleService.updateStatus(id, { status })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['my-schedules'] })
      queryClient.invalidateQueries({ queryKey: ['upcoming-schedules'] })
    }
  })

  const handleStartTrip = (id: number) => {
    updateStatusMutation.mutate({ id, status: 'IN_PROGRESS' })
  }

  const handleCompleteTrip = (id: number) => {
    updateStatusMutation.mutate({ id, status: 'COMPLETED' })
  }

  const columns: Column<DriverSchedule>[] = [
    {
      key: 'id',
      header: 'ID',
      render: (item) => <span className="font-mono text-xs">#{item.id}</span>
    },
    {
      key: 'requesterName',
      header: 'Pemohon',
      render: (item) => (
        <div>
          <p className="font-medium text-gray-900">{item.requesterName}</p>
          <p className="text-sm text-gray-500">{item.purpose}</p>
        </div>
      )
    },
    {
      key: 'destination',
      header: 'Destinasi',
      render: (item) => <span className="text-sm">{item.destination}</span>
    },
    {
      key: 'departureDate',
      header: 'Tanggal',
      render: (item) => (
        <div>
          <p className="text-sm">{formatDate(item.departureDate)}</p>
          <p className="text-xs text-gray-500">
            {item.departureTime} - {item.returnTime}
          </p>
        </div>
      )
    },
    {
      key: 'vehiclePlateNumber',
      header: 'Kendaraan',
      render: (item) => (
        <div>
          <p className="font-medium text-gray-900">{item.vehiclePlateNumber}</p>
          <p className="text-sm text-gray-500">{item.vehicleModel}</p>
        </div>
      )
    },
    {
      key: 'status',
      header: 'Status',
      render: (item) => <Badge status={item.status} />
    },
    {
      key: 'actions',
      header: 'Aksi',
      render: (item) => (
        <div className="flex items-center gap-2">
          {item.status === 'SCHEDULED' && (
            <Button
              variant="secondary"
              size="sm"
              onClick={() => handleStartTrip(item.id)}
              isLoading={updateStatusMutation.isPending}
              leftIcon={<Play className="w-4 h-4" />}
            >
              Mulai
            </Button>
          )}
          {item.status === 'IN_PROGRESS' && (
            <Button
              variant="primary"
              size="sm"
              onClick={() => handleCompleteTrip(item.id)}
              isLoading={updateStatusMutation.isPending}
              leftIcon={<CheckCircle className="w-4 h-4" />}
            >
              Selesai
            </Button>
          )}
        </div>
      )
    }
  ]

  if (isLoading) return <PageLoading />

  if (error) {
    return (
      <Alert variant="error">
        Gagal memuat data jadwal
      </Alert>
    )
  }

  const stats = {
    total: schedules?.length || 0,
    scheduled: schedules?.filter(s => s.status === 'SCHEDULED').length || 0,
    inProgress: schedules?.filter(s => s.status === 'IN_PROGRESS').length || 0,
    completed: schedules?.filter(s => s.status === 'COMPLETED').length || 0
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Jadwal Saya</h1>
        <p className="text-gray-600">Kelola jadwal perjalanan yang ditugaskan kepada Anda</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <Card>
          <CardContent className="py-4">
            <p className="text-sm text-gray-500">Total Jadwal</p>
            <p className="text-2xl font-bold text-gray-900">{stats.total}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="py-4">
            <p className="text-sm text-gray-500">Terjadwal</p>
            <p className="text-2xl font-bold text-blue-600">{stats.scheduled}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="py-4">
            <p className="text-sm text-gray-500">Dalam Perjalanan</p>
            <p className="text-2xl font-bold text-purple-600">{stats.inProgress}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="py-4">
            <p className="text-sm text-gray-500">Selesai</p>
            <p className="text-2xl font-bold text-green-600">{stats.completed}</p>
          </CardContent>
        </Card>
      </div>

      {/* Active Trip Alert */}
      {stats.inProgress > 0 && (
        <Alert variant="info" title="Perjalanan Aktif">
          Anda memiliki {stats.inProgress} perjalanan yang sedang berlangsung. 
          Jangan lupa untuk menandai selesai setelah perjalanan berakhir.
        </Alert>
      )}

      {/* Table */}
      <Card>
        <CardHeader>
          <CardTitle>Daftar Jadwal</CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          <Table
            columns={columns}
            data={schedules || []}
            keyExtractor={(item) => item.id}
            emptyMessage="Belum ada jadwal yang ditugaskan"
          />
        </CardContent>
      </Card>
    </div>
  )
}
